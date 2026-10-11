import AVFoundation
import Speech

nonisolated protocol SpeechTranscribing: Sendable {
    func transcribe(fileURL: URL) async throws -> String
}

/// Uses the same on-device SpeechAnalyzer pipeline as the supplied demo.
nonisolated struct SpeechTranscriptionService: SpeechTranscribing {
    @concurrent
    func transcribe(fileURL: URL) async throws -> String {
        guard SpeechTranscriber.isAvailable,
              let locale = await SpeechTranscriber.supportedLocale(equivalentTo: .current) else {
            throw BlockerSpeechError.speechUnavailable
        }
        let transcriber = SpeechTranscriber(locale: locale, preset: .transcription)
        if let request = try await AssetInventory.assetInstallationRequest(supporting: [transcriber]) {
            try await request.downloadAndInstall()
        }
        try Task.checkCancellation()
        return try await analyze(fileURL, transcriber: transcriber)
    }

    private func feed(_ fileURL: URL, analyzer: SpeechAnalyzer) async throws {
        do {
            _ = try await analyzer.analyzeSequence(from: AVAudioFile(forReading: fileURL))
            try await analyzer.finalizeAndFinishThroughEndOfInput()
        } catch {
            await analyzer.cancelAndFinishNow()
            throw error
        }
    }

    private func analyze(_ fileURL: URL, transcriber: SpeechTranscriber) async throws -> String {
        let analyzer = SpeechAnalyzer(modules: [transcriber])
        return try await withTaskCancellationHandler {
            do {
                async let transcript = transcriber.results.reduce(into: "") { text, result in
                    text += String(result.text.characters)
                }
                try await feed(fileURL, analyzer: analyzer)
                let text = try await transcript.trimmingCharacters(in: .whitespacesAndNewlines)
                try Task.checkCancellation()
                guard !text.isEmpty else { throw BlockerSpeechError.emptyTranscript }
                return text
            } catch {
                await analyzer.cancelAndFinishNow()
                throw error
            }
        } onCancel: {
            Task { await analyzer.cancelAndFinishNow() }
        }
    }
}
