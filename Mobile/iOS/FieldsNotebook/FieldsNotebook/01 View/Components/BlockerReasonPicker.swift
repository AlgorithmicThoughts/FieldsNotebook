import SwiftUI

struct BlockerReasonPicker: View {
    @Binding var reason: String

    var body: some View {
        NotebookSectionCard(title: "Reason", symbol: "doc.text.magnifyingglass") {
            Text("Choose the cause of the delay.").font(.caption).foregroundStyle(NotebookStyle.muted)
            LazyVGrid(columns: [GridItem(.adaptive(minimum: 150), spacing: 8)], spacing: 8) {
                ForEach(DelayReasons.all, id: \.self) { item in
                    Button { reason = item } label: {
                        Label(item, systemImage: BlockerFixtures.reasonSymbol(item))
                            .font(.subheadline.weight(.medium)).fixedSize(horizontal: false, vertical: true)
                            .frame(maxWidth: .infinity, minHeight: 44, alignment: .leading).padding(10)
                            .foregroundStyle(reason == item ? NotebookStyle.accent : NotebookStyle.text)
                            .background(reason == item ? NotebookStyle.soft : NotebookStyle.bg, in: RoundedRectangle(cornerRadius: 10))
                            .overlay(RoundedRectangle(cornerRadius: 10).stroke(reason == item ? NotebookStyle.accent : NotebookStyle.control))
                    }.buttonStyle(.plain).accessibilityAddTraits(reason == item ? .isSelected : [])
                }
            }
        }
    }
}
