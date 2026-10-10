import SwiftUI

struct NotificationRow: View {
    let notice: NotebookNotice
    let open: () -> Void

    var body: some View {
        Button(action: open) {
            HStack(alignment: .top, spacing: 16) {
                Image(systemName: "bell").font(.title3).foregroundStyle(NotebookStyle.accent)
                    .frame(width: 42, height: 42)
                    .background(NotebookStyle.soft, in: RoundedRectangle(cornerRadius: 11))
                VStack(alignment: .leading, spacing: 10) {
                    HStack {
                        Text(notice.isUnread ? "Unread" : "Read").font(.caption.weight(.semibold))
                            .foregroundStyle(NotebookStyle.accent)
                        Spacer()
                        Text(notice.timeLabel).font(.caption).foregroundStyle(NotebookStyle.muted)
                    }
                    Text(notice.title).font(.subheadline.weight(.semibold))
                        .fixedSize(horizontal: false, vertical: true)
                    Text("\(notice.record.project.name) · \(notice.record.id)")
                        .font(.caption).foregroundStyle(NotebookStyle.muted)
                    Label("Open delay", systemImage: "arrow.right").font(.subheadline.weight(.semibold))
                        .foregroundStyle(NotebookStyle.accent)
                }
            }
            .padding(20).frame(maxWidth: .infinity, alignment: .leading).contentShape(Rectangle())
            .background(notice.isUnread ? NotebookStyle.soft : .clear)
        }
        .buttonStyle(.plain)
        .accessibilityElement(children: .combine)
    }
}
