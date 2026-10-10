import SwiftUI

struct ReportSummaryCard: View {
    let report: NotebookReport

    var body: some View {
        NotebookSectionCard(title: "Selected projects", symbol: "building.2") {
            ViewThatFits(in: .horizontal) {
                HStack(spacing: 28) { metrics }
                VStack(alignment: .leading, spacing: 20) { metrics }
            }
            Text("\(report.request.projectIds.count) selected · All dates · Sample data")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
        }
    }

    private var metrics: some View {
        Group {
            ReportCountMetric(title: "Delay records", count: report.records.count)
            ReportCountMetric(title: "Unresolved", count: report.activeCount)
            ReportCountMetric(title: "High priority", count: report.highPriorityCount)
        }
    }
}
