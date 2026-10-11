import SwiftUI

struct BlockerRecordingControls: View {
    let viewModel: BlockerRecordingViewModel
    let send: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Label(status, systemImage: viewModel.state == .recording ? "waveform" : "mic")
                .font(.subheadline.weight(.semibold))
            if let ends = viewModel.recordingEnds, viewModel.state == .recording {
                ProgressView(timerInterval: ends.addingTimeInterval(-30)...ends, countsDown: true)
                    .accessibilityLabel("Recording time remaining")
            }
            Text("Up to 30 seconds. Send transcribes using Apple Speech and suggests report fields. Apple Intelligence is used when available.")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
            ViewThatFits(in: .horizontal) {
                HStack(spacing: 12) { controls }
                VStack(alignment: .leading, spacing: 12) { controls }
            }
        }
        .padding(16).frame(maxWidth: .infinity, alignment: .leading)
        .background(NotebookStyle.soft, in: RoundedRectangle(cornerRadius: 12))
    }

    private var status: String { viewModel.state.rawValue }

    @ViewBuilder
    private var controls: some View {
        if viewModel.state == .recording {
            Button(action: viewModel.stop) { Label("Stop", systemImage: "stop.circle") }.buttonStyle(.bordered)
        } else if viewModel.state == .idle || viewModel.state == .ready {
            Button(action: viewModel.start) { Label("Start recording", systemImage: "mic") }.buttonStyle(.bordered)
        } else { ProgressView() }
        Button("Cancel", action: viewModel.cancel).buttonStyle(.bordered).disabled(viewModel.state == .cancelling)
        Button(action: send) { Label("Send", systemImage: "paperplane") }
            .buttonStyle(NotebookPrimaryButton()).disabled(!viewModel.canSend)
    }
}
