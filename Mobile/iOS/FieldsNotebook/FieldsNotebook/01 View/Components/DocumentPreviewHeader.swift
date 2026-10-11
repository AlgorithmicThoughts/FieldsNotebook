import SwiftUI

struct DocumentPreviewHeader: View {
    let document: SiteDocument

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(document.project.name).font(.subheadline.weight(.semibold))
            Text(document.revision).font(.caption).foregroundStyle(NotebookStyle.muted)
            Text("Sample reference · Use the approved site document for actual work.")
                .font(.caption).foregroundStyle(NotebookStyle.muted)
        }.frame(maxWidth: .infinity, alignment: .leading)
    }
}
