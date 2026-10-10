import SwiftUI

struct DocumentCard: View {
    let document: SiteDocument
    let open: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Image(systemName: document.symbol).font(.title2).foregroundStyle(NotebookStyle.accent)
                .frame(width: 48, height: 48).background(NotebookStyle.soft, in: RoundedRectangle(cornerRadius: 12))
            Text(document.category.rawValue.uppercased()).font(.caption.weight(.bold)).tracking(1)
                .foregroundStyle(NotebookStyle.muted)
            Text(document.title).font(.title3.weight(.semibold))
            Text(document.note).font(.subheadline).foregroundStyle(NotebookStyle.muted)
            Text(document.revision).font(.caption).foregroundStyle(NotebookStyle.muted)
            Button(action: open) {
                Label("View document", systemImage: "arrow.up.right").frame(maxWidth: .infinity)
            }
            .buttonStyle(NotebookPrimaryButton())
            .accessibilityLabel("View \(document.title)")
        }
        .padding(22).frame(maxWidth: .infinity, alignment: .leading).notebookCard()
    }
}
