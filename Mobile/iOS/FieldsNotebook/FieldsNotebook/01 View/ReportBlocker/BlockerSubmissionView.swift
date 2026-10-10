import SwiftUI

struct BlockerSubmissionView: View {
    let record: DelayRecord
    let viewTracking: () -> Void
    let done: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 24) {
            Label("Blocker reported", systemImage: "checkmark.circle.fill").font(.title2.weight(.bold)).foregroundStyle(NotebookStyle.accent)
            Text("\(record.id) · \(record.project.name)").font(.headline)
            Text(record.detail)
            NotebookInfoRow(title: "Responsible → Affected", detail: record.summary.crewSummary, symbol: "person.2")
            Text("This sample report is now in Home and Tracking for this demo session. No notifications were sent.")
                .font(.subheadline).foregroundStyle(NotebookStyle.muted)
            Button(action: viewTracking) { Label("View in Tracking", systemImage: "list.bullet.rectangle") }.buttonStyle(NotebookPrimaryButton())
            Button("Done", action: done).frame(minHeight: 44)
        }.padding(28).frame(maxWidth: 700, alignment: .leading)
    }
}
