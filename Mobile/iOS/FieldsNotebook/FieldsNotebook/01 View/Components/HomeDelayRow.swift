import SwiftUI

struct HomeDelayRow: View {
    let delay: HomeDelay
    @Environment(\.horizontalSizeClass) private var sizeClass

    var body: some View {
        let colors = delay.isHighPriority ? (NotebookStyle.urgent, NotebookStyle.urgentBG) : (NotebookStyle.accent, NotebookStyle.soft)
        HStack(alignment: .top, spacing: 14) {
            if sizeClass == .regular {
                Image(systemName: delay.isHighPriority ? "pin.fill" : delay.reasonSymbol)
                    .font(.title3).foregroundStyle(colors.0)
                    .frame(width: 40, height: 40)
                    .background(colors.1, in: RoundedRectangle(cornerRadius: 11))
            }
            VStack(alignment: .leading, spacing: 8) {
                Text("\(delay.id) · \(delay.reason)").font(.caption).foregroundStyle(NotebookStyle.muted)
                if delay.isHighPriority {
                    Label("High priority · Reopened", systemImage: "pin.fill")
                        .font(.caption.weight(.semibold)).foregroundStyle(NotebookStyle.urgent)
                }
                Text(delay.title).font(.subheadline.weight(.semibold))
                    .fixedSize(horizontal: false, vertical: true)
                Text("\(delay.crewSummary) · \(delay.location)")
                    .font(.caption).foregroundStyle(NotebookStyle.muted)
                HomeStatusBadge(status: delay.status)
            }
            .frame(maxWidth: .infinity, alignment: .leading)
            Image(systemName: "chevron.right").font(.caption.weight(.semibold))
                .foregroundStyle(NotebookStyle.muted).padding(.top, 4)
        }
        .padding(20).contentShape(Rectangle())
        .accessibilityElement(children: .combine)
    }
}

