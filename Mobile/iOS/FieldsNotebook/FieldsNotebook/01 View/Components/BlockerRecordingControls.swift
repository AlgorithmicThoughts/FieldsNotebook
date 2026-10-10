import SwiftUI

struct BlockerRecordingControls: View {
    let viewModel: BlockerRecordingViewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Label(status, systemImage: viewModel.state == .recording ? "waveform" : "mic")
                .font(.subheadline.weight(.semibold)).accessibilityAddTraits(.updatesFrequently)
            Text("UI preview · Audio capture and Apple Speech will be added later.")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
            ViewThatFits(in: .horizontal) {
                HStack(spacing: 12) { controls }
                VStack(alignment: .leading, spacing: 12) { controls }
            }
        }
        .padding(16).frame(maxWidth: .infinity, alignment: .leading)
        .background(NotebookStyle.soft, in: RoundedRectangle(cornerRadius: 12))
    }

    private var status: String {
        if viewModel.state == .recording { return "Recording preview in progress" }
        return viewModel.state == .ready ? "Ready to send recording preview" : "Record a description"
    }

    @ViewBuilder
    private var controls: some View {
        if viewModel.state == .recording {
            Button(action: viewModel.stop) { Label("Stop", systemImage: "stop.circle") }.buttonStyle(.bordered)
        } else {
            Button(action: viewModel.start) { Label("Start recording", systemImage: "mic") }.buttonStyle(.bordered)
        }
        Button("Cancel", action: viewModel.cancel).buttonStyle(.bordered)
        Button(action: viewModel.send) { Label("Send", systemImage: "paperplane") }
            .buttonStyle(NotebookPrimaryButton()).disabled(!viewModel.canSend)
    }
}
