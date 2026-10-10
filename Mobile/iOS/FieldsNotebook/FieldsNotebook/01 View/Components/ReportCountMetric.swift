import SwiftUI

struct ReportCountMetric: View {
    let title: String
    let count: Int

    var body: some View {
        VStack(alignment: .leading, spacing: 5) {
            Text(count, format: .number).font(.largeTitle.weight(.bold))
            Text(title).font(.caption).foregroundStyle(NotebookStyle.muted)
        }
        .accessibilityElement(children: .combine)
    }
}
