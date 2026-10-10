import SwiftUI

struct NotebookRefreshFooter: View {
    let lastChecked: Date
    let refresh: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Button(action: refresh) {
                Label("Last check \(lastChecked.formatted(Self.checkTime)) · Refresh", systemImage: "arrow.clockwise")
                    .font(.caption).frame(minHeight: 44)
            }
            Text("Sample data · 7 Oct 2026 · Project time: Central")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
        }
    }

    private static var checkTime: Date.FormatStyle {
        Date.FormatStyle(date: .omitted, time: .shortened, timeZone: TimeZone(identifier: "America/Chicago")!)
    }
}
