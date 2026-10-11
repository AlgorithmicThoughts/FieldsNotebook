import AVFoundation

@MainActor
protocol BlockerAudioRecording: AnyObject {
    func start() async throws
    func stop() async throws -> URL
    func cancel() async
}

@MainActor
final class BlockerAudioRecorder: BlockerAudioRecording {
    private var recorder: AVAudioRecorder?
    private var fileURL: URL?

    func start() async throws {
        guard await AVAudioApplication.requestRecordPermission() else { throw BlockerSpeechError.microphoneDenied }
        try Task.checkCancellation()
        do {
            try AVAudioSession.sharedInstance().setCategory(.record, mode: .default)
            try await Self.activate()
            try Task.checkCancellation()
            try beginRecording()
        } catch {
            await cancel()
            throw error
        }
    }

    private func beginRecording() throws {
        let url = URL.temporaryDirectory.appending(path: "blocker-\(UUID().uuidString).m4a")
        fileURL = url
        recorder = try AVAudioRecorder(url: url, settings: [
            AVFormatIDKey: kAudioFormatMPEG4AAC, AVSampleRateKey: 16_000, AVNumberOfChannelsKey: 1
        ])
        guard recorder?.record() == true else { throw BlockerSpeechError.recordingFailed }
    }

    func stop() async throws -> URL {
        recorder?.stop()
        recorder = nil
        try await Self.deactivate()
        guard let url = fileURL else { throw BlockerSpeechError.recordingFailed }
        fileURL = nil
        return url
    }

    func cancel() async {
        recorder?.stop()
        recorder = nil
        try? await Self.deactivate()
        if let fileURL { try? FileManager.default.removeItem(at: fileURL) }
        fileURL = nil
    }

    @concurrent private static func activate() async throws { try await AVAudioSession.sharedInstance().activate() }
    @concurrent private static func deactivate() async throws { try await AVAudioSession.sharedInstance().deactivate() }
}
