import SwiftUI

struct NotebookSidebarItem: View {
    let tab: NotebookTab
    let selected: Bool
    let unreadCount: Int
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            HStack(spacing: 12) {
                Image(systemName: tab.symbol).frame(width: 22)
                Text(tab.rawValue).font(.subheadline.weight(.semibold))
                Spacer(minLength: 0)
                if tab == .notifications && unreadCount > 0 {
                    Text(unreadCount, format: .number).font(.caption.weight(.semibold)).padding(5)
                        .background(NotebookStyle.bg, in: RoundedRectangle(cornerRadius: 6))
                }
            }
            .padding(.horizontal, 12).frame(minHeight: 48)
            .foregroundStyle(colors.0)
            .background(colors.1, in: RoundedRectangle(cornerRadius: 10))
            .contentShape(Rectangle())
        }
        .buttonStyle(.plain)
        .accessibilityAddTraits(selected ? .isSelected : [])
    }

    private var colors: (Color, Color) {
        selected ? (NotebookStyle.accent, NotebookStyle.soft) : (NotebookStyle.muted, .clear)
    }
}
