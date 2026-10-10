import SwiftUI

struct NotebookBrand: View {
    var body: some View {
        HStack(spacing: 10) {
            Image(systemName: "arrow.right").font(.headline)
                .foregroundStyle(NotebookStyle.onAccent)
                .frame(width: 36, height: 36)
                .background(NotebookStyle.accent, in: RoundedRectangle(cornerRadius: 10))
            Text("Field Notebook").font(.headline)
        }
        .foregroundStyle(NotebookStyle.text)
    }
}
