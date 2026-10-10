import SwiftUI

struct NotebookSearchField: View {
    let placeholder: String
    @Binding var query: String

    var body: some View {
        HStack(spacing: 10) {
            Image(systemName: "magnifyingglass").foregroundStyle(NotebookStyle.muted)
            TextField(placeholder, text: $query)
                .textInputAutocapitalization(.never).autocorrectionDisabled()
                .accessibilityLabel(placeholder)
            if !query.isEmpty {
                Button { query = "" } label: {
                    Image(systemName: "xmark.circle.fill").frame(width: 44, height: 44)
                }
                .accessibilityLabel("Clear search")
            }
        }
        .padding(.leading, 14).padding(.trailing, 4).frame(minHeight: 48)
        .background(NotebookStyle.surface, in: RoundedRectangle(cornerRadius: 10))
        .overlay(RoundedRectangle(cornerRadius: 10).stroke(NotebookStyle.control, lineWidth: 1))
    }
}
