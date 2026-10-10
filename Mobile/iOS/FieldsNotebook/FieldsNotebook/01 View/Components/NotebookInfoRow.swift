import SwiftUI

struct NotebookInfoRow: View {
    let title: String
    let detail: String
    let symbol: String
    var value: String?

    var body: some View {
        HStack(alignment: .top, spacing: 16) {
            VStack(alignment: .leading, spacing: 6) {
                Label(title, systemImage: symbol).font(.subheadline.weight(.semibold))
                Text(detail).font(.caption).foregroundStyle(NotebookStyle.muted)
                    .fixedSize(horizontal: false, vertical: true)
            }
            Spacer(minLength: 8)
            if let value {
                Text(value).font(.caption.weight(.semibold))
                    .foregroundStyle(NotebookStyle.accent)
                    .padding(.horizontal, 10).padding(.vertical, 6)
                    .background(NotebookStyle.soft, in: Capsule())
            }
        }
        .frame(minHeight: 44)
        .accessibilityElement(children: .combine)
    }
}
