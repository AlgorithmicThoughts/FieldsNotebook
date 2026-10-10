import SwiftUI

struct BlockerTimingFields: View {
    @Binding var draft: BlockerDraft

    var body: some View {
        NotebookSectionCard(title: "Timing", symbol: "calendar") {
            Toggle("Report before the other crew arrives", isOn: $draft.advanceNotice).frame(minHeight: 44)
            if draft.advanceNotice {
                DatePicker("Tentative completion", selection: $draft.tentativeCompletion)
                    .frame(minHeight: 44).environment(\.timeZone, TimeZone(identifier: "America/Chicago")!)
                Text("Project time: Central. The responsible Superintendent must confirm this estimate.")
                    .font(.caption).foregroundStyle(NotebookStyle.muted)
            } else {
                Text("This blocker is stopping the work now.").font(.caption).foregroundStyle(NotebookStyle.muted)
            }
            Text("An estimate does not clear a blocker or authorize a handover.")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
        }
    }
}
