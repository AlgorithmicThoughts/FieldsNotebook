import SwiftUI

struct HomeMetricCard: View {
    let metric: HomeMetric
    let count: Int
    let wide: Bool

    var body: some View {
        HStack(spacing: 14) {
            if wide {
                Image(systemName: metric.symbol).font(.title3)
                    .foregroundStyle(NotebookStyle.accent)
                    .frame(width: 42, height: 42)
                    .background(NotebookStyle.soft, in: RoundedRectangle(cornerRadius: 11))
            }
            VStack(alignment: .leading, spacing: 5) {
                Text(count, format: .number).font(.largeTitle.weight(.bold))
                Text(metric.rawValue).font(.caption).foregroundStyle(NotebookStyle.muted)
                    .frame(minHeight: 30, alignment: .top)
            }
            Spacer(minLength: 0)
        }
        .padding(wide ? 20 : 12)
        .frame(maxWidth: .infinity, alignment: .leading)
        .notebookCard()
        .accessibilityElement(children: .combine)
    }
}
