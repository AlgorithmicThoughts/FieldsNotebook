import SwiftUI

struct NotebookSectionCard<Content: View>: View {
    let title: String
    let symbol: String
    @ViewBuilder let content: () -> Content

    var body: some View {
        VStack(alignment: .leading, spacing: 20) {
            Label(title, systemImage: symbol).font(.headline)
                .foregroundStyle(NotebookStyle.text)
            content()
        }
        .padding(22).frame(maxWidth: .infinity, alignment: .leading).notebookCard()
    }
}
