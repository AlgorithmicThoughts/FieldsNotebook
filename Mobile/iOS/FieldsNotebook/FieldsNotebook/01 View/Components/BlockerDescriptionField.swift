import SwiftUI

struct BlockerDescriptionField: View {
    @Bindable var viewModel: ReportBlockerViewModel
    private var recording: BlockerRecordingViewModel { viewModel.recording }

    var body: some View {
        NotebookSectionCard(title: "What stops the work?", symbol: "doc.text") {
            HStack {
                Text("Describe the blocker and the work it stops.").font(.subheadline).foregroundStyle(NotebookStyle.muted)
                Spacer(minLength: 12)
                Button(action: recording.show) { Label("Speak", systemImage: "mic").frame(minHeight: 44) }
            }
            BlockerTitleField(title: $viewModel.draft.title)
            TextField("Describe the blocker and the work it stops.", text: $viewModel.draft.description, axis: .vertical)
                .lineLimit(5...8).frame(minHeight: 120, alignment: .topLeading).padding(12)
                .background(NotebookStyle.bg)
                .clipShape(RoundedRectangle(cornerRadius: 10))
                .overlay(RoundedRectangle(cornerRadius: 10).stroke(NotebookStyle.control))
                .accessibilityLabel("Blocker description")
                .onChange(of: viewModel.draft.description) { _, value in viewModel.draft.description = String(value.prefix(4000)) }
            Text("\(viewModel.draft.description.count)/4,000 characters").font(.caption).foregroundStyle(NotebookStyle.muted)
                .frame(maxWidth: .infinity, alignment: .trailing)
            if recording.isExpanded { BlockerRecordingControls(viewModel: recording, send: viewModel.sendRecording).id("blockerRecording") }
            if let message = recording.message { Text(message).font(.caption).foregroundStyle(NotebookStyle.muted) }
            if let transcript = recording.transcript {
                DisclosureGroup("Recognized speech") { Text(transcript).font(.subheadline).textSelection(.enabled) }
            }
        }
    }
}
