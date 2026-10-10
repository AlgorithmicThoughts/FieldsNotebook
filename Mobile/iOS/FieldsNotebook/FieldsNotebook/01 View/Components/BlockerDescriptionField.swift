import SwiftUI

struct BlockerDescriptionField: View {
    @Binding var description: String
    @State private var recording = BlockerRecordingViewModel()
    @Environment(\.scenePhase) private var scenePhase

    var body: some View {
        NotebookSectionCard(title: "What stops the work?", symbol: "doc.text") {
            HStack {
                Text("Describe the blocker and the work it stops.").font(.subheadline).foregroundStyle(NotebookStyle.muted)
                Spacer(minLength: 12)
                Button(action: recording.show) { Label("Speak", systemImage: "mic").frame(minHeight: 44) }
            }
            TextField("Describe the blocker and the work it stops.", text: $description, axis: .vertical)
                .lineLimit(5...8).frame(minHeight: 120, alignment: .topLeading).padding(12)
                .background(NotebookStyle.bg)
                .clipShape(RoundedRectangle(cornerRadius: 10))
                .overlay(RoundedRectangle(cornerRadius: 10).stroke(NotebookStyle.control))
                .accessibilityLabel("Blocker description")
                .onChange(of: description) { _, value in description = String(value.prefix(4000)) }
            Text("\(description.count)/4,000 characters").font(.caption).foregroundStyle(NotebookStyle.muted)
                .frame(maxWidth: .infinity, alignment: .trailing)
            if recording.isExpanded { BlockerRecordingControls(viewModel: recording) }
            if let message = recording.message { Text(message).font(.caption).foregroundStyle(NotebookStyle.muted) }
        }
        .onDisappear { recording.cancel() }
        .onChange(of: scenePhase) { _, phase in if phase != .active { recording.cancel() } }
    }
}
