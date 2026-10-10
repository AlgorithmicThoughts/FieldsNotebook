import SwiftUI

struct ReportReasonChart: View {
    let report: NotebookReport

    var body: some View {
        NotebookSectionCard(title: "Delays by reason", symbol: "chart.bar") {
            Text("Counts delay records, including resolved records. No delay-hour estimates.")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
            ForEach(report.reasonCounts) { item in
                VStack(spacing: 7) {
                    HStack {
                        Text(item.reason).font(.subheadline.weight(.medium))
                        Spacer()
                        Text(item.count, format: .number).font(.subheadline.weight(.semibold))
                    }
                    ProgressView(value: Double(item.count), total: Double(max(report.records.count, 1)))
                        .tint(NotebookStyle.accent)
                }
                .accessibilityElement(children: .ignore)
                .accessibilityLabel("\(item.reason): \(item.count) of \(report.records.count) delay records")
            }
        }
    }
}
