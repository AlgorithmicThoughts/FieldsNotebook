import SwiftUI

struct DelayRecordRow: View {
    let record: DelayRecord

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            HomeDelayRow(delay: record.summary)
            Label(record.project.name, systemImage: "building.2")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
                .padding(.horizontal, 20).padding(.bottom, 18)
        }
        .frame(maxWidth: .infinity, alignment: .leading).contentShape(Rectangle())
        .accessibilityElement(children: .combine)
    }
}
