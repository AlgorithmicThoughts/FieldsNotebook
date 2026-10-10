import SwiftUI

struct DelaySummaryView: View {
    let delay: HomeDelay
    let projectName: String
    var detail: String?
    var completion: String?
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 18) {
                    Text(projectName).font(.subheadline).foregroundStyle(NotebookStyle.muted)
                    Text(delay.title).font(.title2.weight(.bold))
                    HomeStatusBadge(status: delay.status)
                    if let detail { Text(detail).font(.body) }
                    if delay.isHighPriority {
                        Label("High priority · Reopened", systemImage: "pin.fill").foregroundStyle(NotebookStyle.urgent)
                    }
                    Label(delay.reason, systemImage: delay.reasonSymbol)
                    Label(delay.crewSummary, systemImage: "person.2")
                    Label(delay.location, systemImage: "mappin.and.ellipse")
                    if let completion {
                        NotebookCallout(title: delay.isActive ? "Tentative completion" : "Handover complete",
                                        message: completion, symbol: "clock")
                    }
                    Text(handoverNote)
                        .font(.subheadline).foregroundStyle(NotebookStyle.muted)
                }
                .padding(24).frame(maxWidth: .infinity, alignment: .leading)
            }
            .background(NotebookStyle.bg)
            .navigationTitle(delay.id).navigationBarTitleDisplayMode(.inline)
            .toolbar { ToolbarItem(placement: .confirmationAction) { Button("Done") { dismiss() } } }
        }
        .presentationDetents([.medium, .large])
        .preferredColorScheme(.light)
    }

    private var handoverNote: String {
        delay.isActive
            ? "The affected crew’s Foreman must verify the handover before the responsible Superintendent resolves the delay."
            : "The affected crew’s Foreman verified the handover and the responsible Superintendent resolved this delay."
    }
}
