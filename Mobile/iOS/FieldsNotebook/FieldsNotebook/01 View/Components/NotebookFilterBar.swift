import SwiftUI

struct NotebookFilterBar<Option: RawRepresentable & Identifiable & Hashable>: View where Option.RawValue == String {
    let options: [Option]
    @Binding var selection: Option

    var body: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                ForEach(options) { option in
                    Button { selection = option } label: {
                        Text(option.rawValue).font(.subheadline.weight(.semibold))
                            .padding(.horizontal, 15).frame(minHeight: 44)
                            .foregroundStyle(selection == option ? NotebookStyle.accent : NotebookStyle.muted)
                            .background(selection == option ? NotebookStyle.soft : NotebookStyle.surface, in: Capsule())
                    }
                    .buttonStyle(.plain)
                    .accessibilityAddTraits(selection == option ? .isSelected : [])
                }
            }
        }
    }
}
