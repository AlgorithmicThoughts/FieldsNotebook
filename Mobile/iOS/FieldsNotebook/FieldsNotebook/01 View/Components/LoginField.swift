import SwiftUI

struct LoginField<Content: View>: View {
    let label: String
    @ViewBuilder let content: () -> Content

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(label).font(.subheadline.weight(.semibold))
            content()
                .font(.body).padding(.horizontal, 14).frame(minHeight: 52)
                .background(NotebookStyle.bg, in: RoundedRectangle(cornerRadius: 10))
                .overlay(RoundedRectangle(cornerRadius: 10).stroke(NotebookStyle.border, lineWidth: 1))
                .accessibilityLabel(label)
        }
    }
}
