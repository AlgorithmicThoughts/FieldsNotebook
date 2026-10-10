import Foundation
import Observation

/// UI preview only. Audio capture and Apple Speech transcription are deferred.
@MainActor
@Observable
final class BlockerRecordingViewModel {
    private(set) var state: BlockerRecordingState = .idle
    private(set) var isExpanded = false
    private(set) var message: String?
    var canSend: Bool { state != .idle }

    func show() { isExpanded = true; message = nil }
    func start() { state = .recording; message = nil }
    func stop() { state = .ready }
    func cancel() { state = .idle; isExpanded = false; message = nil }
    func send() {
        guard canSend else { return }
        state = .idle
        isExpanded = false
        message = "Recording preview sent. Speech-to-text will be connected later; type your description for now."
    }
}
