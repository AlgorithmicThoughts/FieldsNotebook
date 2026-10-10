import SwiftUI

struct AdvanceNoticeCard: View {
    let notice: HomeAdvanceNotice
    let wide: Bool
    let openDelay: () -> Void

    var body: some View {
        Group {
            if wide {
                HStack(spacing: 0) {
                    content.frame(maxWidth: .infinity, alignment: .leading)
                    estimate.frame(width: 220)
                }
            } else {
                VStack(alignment: .leading, spacing: 0) {
                    content
                    estimate.frame(maxWidth: .infinity, alignment: .leading)
                }
            }
        }
        .notebookCard()
    }

    private var content: some View {
        VStack(alignment: .leading, spacing: 15) {
            Label("AHEAD OF TOMORROW", systemImage: "calendar")
                .font(.caption.weight(.bold)).tracking(0.7)
                .padding(.horizontal, 10).padding(.vertical, 7)
                .foregroundStyle(NotebookStyle.blocked)
                .background(NotebookStyle.blockedBG, in: Capsule())
            Text(notice.headline).font(.title2.weight(.bold))
                .fixedSize(horizontal: false, vertical: true)
            Text(notice.detail).font(.subheadline).foregroundStyle(NotebookStyle.muted)
            Button(action: openDelay) { Label("View delay", systemImage: "arrow.right") }
                .buttonStyle(NotebookPrimaryButton())
            Text(notice.delayID + " · Crew A → Crew B").font(.caption).foregroundStyle(NotebookStyle.muted)
        }
        .padding(24).frame(maxWidth: .infinity, alignment: .leading)
        .overlay(alignment: .leading) { Rectangle().fill(NotebookStyle.accent).frame(width: 4) }
    }

    private var estimate: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("TENTATIVE COMPLETION").font(.caption.weight(.bold)).tracking(1)
                .foregroundStyle(NotebookStyle.muted)
            Text(notice.time).font(.largeTitle.weight(.bold))
            Text(notice.day).font(.subheadline.weight(.semibold))
            Label("An estimate is not clearance to start.", systemImage: "lock")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
                .fixedSize(horizontal: false, vertical: true)
        }
        .padding(24).frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .leading)
        .background(NotebookStyle.soft)
    }
}
