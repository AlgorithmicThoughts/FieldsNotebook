import SwiftUI

struct UpcomingWorkCard: View {
    let work: UpcomingWork
    let openDelay: (DelayRecord) -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            VStack(alignment: .leading, spacing: 16) {
                HStack(alignment: .top) {
                    VStack(alignment: .leading, spacing: 5) {
                        Text(work.plannedTime).font(.title2.weight(.bold))
                        Text("Planned start").font(.caption).foregroundStyle(NotebookStyle.muted)
                    }
                    Spacer()
                    HomeStatusBadge(status: .blocked)
                }
                Text("\(work.crew) · \(work.title)").font(.title2.weight(.semibold))
                Text("Tentative handover: \(work.completion)").font(.subheadline).foregroundStyle(NotebookStyle.muted)
                NotebookCallout(title: "Coordinate before arrival",
                                message: "Your Superintendent coordinates the arrival plan. All blockers below must clear before work starts.",
                                symbol: "person.2")
                Text("\(work.blockers.count) unresolved \(work.blockers.count == 1 ? "blocker" : "blockers")").font(.headline)
            }
            .padding(22)
            ForEach(work.blockers) { record in
                Divider().overlay(NotebookStyle.border)
                Button { openDelay(record) } label: { HomeDelayRow(delay: record.summary) }
                    .buttonStyle(.plain)
            }
        }
        .notebookCard()
    }
}
