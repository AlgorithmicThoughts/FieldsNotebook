import SwiftUI

struct DocumentPreviewView: View {
    let document: SiteDocument
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        NavigationStack {
            ScrollView {
                VStack(alignment: .leading, spacing: 20) {
                    Text(document.project.name).font(.subheadline.weight(.semibold))
                    Text(document.revision).font(.caption).foregroundStyle(NotebookStyle.muted)
                    NotebookCallout(title: "Demo reference",
                                    message: "Replace this sample with an approved site document before using it for work.")
                    if document.category == .plans { SampleAreaPlan() }
                    ForEach(document.sections) { section in
                        VStack(alignment: .leading, spacing: 9) {
                            Text(section.title).font(.headline)
                            Text(section.detail).font(.body).foregroundStyle(NotebookStyle.muted)
                        }
                        .padding(20).frame(maxWidth: .infinity, alignment: .leading).notebookCard()
                    }
                }
                .padding(24)
            }
            .background(NotebookStyle.bg)
            .navigationTitle(document.title).navigationBarTitleDisplayMode(.inline)
            .toolbar { ToolbarItem(placement: .confirmationAction) { Button("Done") { dismiss() } } }
        }
        .preferredColorScheme(.light)
    }
}
