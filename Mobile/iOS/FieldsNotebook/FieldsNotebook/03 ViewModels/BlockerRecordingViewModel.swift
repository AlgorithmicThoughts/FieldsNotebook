import Foundation
import Observation

@MainActor
@Observable
final class BlockerRecordingViewModel {
    private(set) var state: BlockerRecordingState = .idle
    private(set) var isExpanded = false
    private(set) var message: String?
    private(set) var transcript: String?
    private(set) var recordingEnds: Date?
    var canSend: Bool { state == .recording || state == .ready }
    var isActive: Bool { state != .idle }
    @ObservationIgnored private let recorder: any BlockerAudioRecording
    @ObservationIgnored private let transcriber: any SpeechTranscribing
    @ObservationIgnored private let filler: any SpokenBlockerFilling
    @ObservationIgnored private var operation: Task<Void, Never>?
    @ObservationIgnored private var countdown: Task<Void, Never>?
    @ObservationIgnored private var operationID = UUID()
    @ObservationIgnored private var fileURL: URL?

    init(recorder: any BlockerAudioRecording, transcriber: any SpeechTranscribing, filler: any SpokenBlockerFilling) {
        self.recorder = recorder
        self.transcriber = transcriber
        self.filler = filler
    }

    func show() { isExpanded = true }

    func start() {
        guard state == .idle || state == .ready else { return }
        deleteRecording()
        transcript = nil
        message = nil
        state = .starting
        launch {
            try await self.recorder.start()
            try Task.checkCancellation()
            self.state = .recording
            self.recordingEnds = .now.addingTimeInterval(30)
            self.startCountdown()
        }
    }

    func stop() {
        guard state == .recording else { return }
        state = .stopping
        countdown?.cancel()
        launch {
            self.fileURL = try await self.recorder.stop()
            try Task.checkCancellation()
            self.state = .ready
            self.recordingEnds = nil
        }
    }

    func send(crews: [SpeechCrewOption], apply: @escaping @MainActor (String, SpokenBlockerFields?) -> Void) {
        guard canSend else { return }
        let wasRecording = state == .recording
        state = .processing
        countdown?.cancel()
        message = "Transcribing on this device. The speech model may download on first use."
        launch {
            if wasRecording { self.fileURL = try await self.recorder.stop() }
            try await self.process(crews: crews, apply: apply)
        }
    }

    func cancel() {
        guard state != .cancelling else { return }
        operationID = UUID()
        let previous = operation
        previous?.cancel()
        countdown?.cancel()
        state = .cancelling
        isExpanded = false
        transcript = nil
        recordingEnds = nil
        message = nil
        operation = Task {
            await previous?.value
            await self.recorder.cancel()
            self.deleteRecording()
            self.state = .idle
        }
    }

    private func process(crews: [SpeechCrewOption], apply: @escaping @MainActor (String, SpokenBlockerFields?) -> Void) async throws {
        guard let url = fileURL else { throw BlockerSpeechError.recordingFailed }
        fileURL = nil
        defer { try? FileManager.default.removeItem(at: url) }
        let text = try await transcriber.transcribe(fileURL: url)
        try Task.checkCancellation()
        transcript = text
        message = "Selecting fields from your spoken note…"
        let suggestions = try await suggestedFields(text, crews: crews)
        try Task.checkCancellation()
        apply(text, suggestions?.fields)
        state = .idle
        recordingEnds = nil
        isExpanded = false
        message = suggestions?.usedAppleIntelligence == true
            ? "Speech filled the report. Review the title, description, responsible crew and reason before sending."
            : "Description transcribed. Automatic selection used explicit phrases because Apple Intelligence was unavailable. Review the crew and reason."
    }

    private func suggestedFields(_ text: String, crews: [SpeechCrewOption]) async throws -> SpokenBlockerSuggestions? {
        do { return try await filler.fields(from: text, crews: crews) }
        catch is CancellationError { throw CancellationError() }
        catch { return nil } // A model failure must not discard a successful transcription.
    }

    private func launch(_ work: @escaping @MainActor () async throws -> Void) {
        let id = UUID()
        operationID = id
        operation = Task {
            do { try await work() }
            catch is CancellationError { return }
            catch {
                guard self.operationID == id else { return }
                await self.recorder.cancel()
                guard self.operationID == id else { return }
                self.deleteRecording()
                self.state = .idle
                self.recordingEnds = nil
                self.message = error.localizedDescription
            }
        }
    }

    private func startCountdown() {
        countdown?.cancel()
        countdown = Task {
            do {
                try await Task.sleep(for: .seconds(30))
                try Task.checkCancellation()
                self.stop()
            } catch { return }
        }
    }

    private func deleteRecording() {
        if let fileURL { try? FileManager.default.removeItem(at: fileURL) }
        fileURL = nil
    }
}
