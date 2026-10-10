import SwiftUI

struct NotebookSettingToggle: View {
    let title: String
    let detail: String
    let symbol: String
    @Binding var isOn: Bool

    var body: some View {
        Toggle(isOn: $isOn) {
            VStack(alignment: .leading, spacing: 6) {
                Label(title, systemImage: symbol).font(.subheadline.weight(.semibold))
                Text(detail).font(.caption).foregroundStyle(NotebookStyle.muted)
                    .fixedSize(horizontal: false, vertical: true)
            }
        }
        .frame(minHeight: 44)
        .tint(NotebookStyle.accent)
    }
}
