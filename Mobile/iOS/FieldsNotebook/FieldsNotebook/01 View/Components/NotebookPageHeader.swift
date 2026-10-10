import SwiftUI

struct NotebookPageHeader: View {
    let projectName: String
    let title: String
    let subtitle: String

    var body: some View {
        VStack(alignment: .leading, spacing: 9) {
            Text(projectName.uppercased()).font(.caption.weight(.bold)).tracking(1.3)
                .foregroundStyle(NotebookStyle.muted)
            Text(title).font(.largeTitle.weight(.bold))
            Text(subtitle).font(.subheadline).foregroundStyle(NotebookStyle.muted)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }
}
