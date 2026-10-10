import SwiftUI

struct NotebookCallout: View {
    let title: String
    let message: String
    var symbol = "info.circle"

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            Image(systemName: symbol).font(.title3).foregroundStyle(NotebookStyle.accent)
            VStack(alignment: .leading, spacing: 6) {
                Text(title).font(.subheadline.weight(.semibold))
                Text(message).font(.subheadline).foregroundStyle(NotebookStyle.muted)
            }
        }
        .padding(18).frame(maxWidth: .infinity, alignment: .leading)
        .background(NotebookStyle.soft, in: RoundedRectangle(cornerRadius: 14))
    }
}
