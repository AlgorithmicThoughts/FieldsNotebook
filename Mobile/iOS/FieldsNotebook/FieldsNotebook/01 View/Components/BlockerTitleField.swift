import SwiftUI

struct BlockerTitleField: View {
    @Binding var title: String

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text("Title (optional)").font(.subheadline.weight(.medium))
            TextField("Short title for this blocker", text: $title)
                .textFieldStyle(.roundedBorder).accessibilityLabel("Blocker title")
                .onChange(of: title) { _, value in title = String(value.prefix(200)) }
        }
    }
}
