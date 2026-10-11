import SwiftUI
import FactoryKit

struct DocumentPreviewView: View {
    @State private var viewModel: DocumentPreviewViewModel
    @Environment(\.dismiss) private var dismiss

    init(document: SiteDocument) {
        _viewModel = State(initialValue: Container.shared.documentPreviewViewModel(document))
    }

    var body: some View {
        NavigationStack {
            VStack(spacing: 0) {
                DocumentPreviewHeader(document: viewModel.document).padding(24)
                Divider()
                preview
            }
            .background(NotebookStyle.bg)
            .navigationTitle(viewModel.document.title).navigationBarTitleDisplayMode(.inline)
            .toolbar { ToolbarItem(placement: .confirmationAction) { Button("Done") { dismiss() } } }
        }
        .preferredColorScheme(.light).presentationDetents([.large]).presentationSizing(.page)
    }

    @ViewBuilder
    private var preview: some View {
        if viewModel.document.pdfResourceName != nil {
            if let pdf = viewModel.pdf { NotebookPDFView(document: pdf) }
            else {
                ContentUnavailableView("Document unavailable", systemImage: "doc.text",
                                       description: Text("The sample PDF could not be opened."))
            }
        } else { referencePreview }
    }

    private var referencePreview: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 20) {
                if viewModel.document.category == .plans { siteLayout }
                ForEach(viewModel.document.sections) { section in
                    VStack(alignment: .leading, spacing: 9) {
                        Text(section.title).font(.headline)
                        Text(section.detail).font(.body).foregroundStyle(NotebookStyle.muted)
                    }
                    .padding(20).frame(maxWidth: .infinity, alignment: .leading).notebookCard()
                }
            }.padding(24)
        }
    }

    @ViewBuilder
    private var siteLayout: some View {
        if let image = viewModel.siteImage {
            SiteMapImage(image: image).clipShape(RoundedRectangle(cornerRadius: 12))
        } else {
            ContentUnavailableView("Site image unavailable", systemImage: "map")
        }
    }
}
