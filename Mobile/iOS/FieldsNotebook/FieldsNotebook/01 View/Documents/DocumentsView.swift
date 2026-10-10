import SwiftUI
import FactoryKit

struct DocumentsView: View {
    let project: HomeProject
    @InjectedObservable(\.documentsViewModel) private var viewModel
    @State private var selectedDocument: SiteDocument?

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 22) {
                NotebookPageHeader(projectName: project.name, title: "Site documents",
                                   subtitle: "Find the current site reference before you start work.")
                NotebookCallout(title: "Sample documents",
                                message: "These previews contain no approved safety instructions or construction plans.")
                NotebookSearchField(placeholder: "Search site documents", query: $viewModel.query)
                NotebookFilterBar(options: DocumentCategory.allCases, selection: $viewModel.category)
                documentGrid
                Text("Documents belong to the selected project. Switch projects to view another site’s references.")
                    .font(.caption).foregroundStyle(NotebookStyle.muted)
            }
            .padding(28).frame(maxWidth: 1250).frame(maxWidth: .infinity)
        }
        .background(NotebookStyle.bg).refreshable { viewModel.refresh() }
        .task(id: project.id) { viewModel.load(project: project) }
        .sheet(item: $selectedDocument) { DocumentPreviewView(document: $0) }
    }

    private var documentGrid: some View {
        Group {
            if viewModel.filteredDocuments.isEmpty {
                ContentUnavailableView("No matching documents", systemImage: "doc.text",
                                       description: Text("Try another category or search."))
            } else {
                LazyVGrid(columns: [GridItem(.adaptive(minimum: 250), spacing: 18)], spacing: 18) {
                    ForEach(viewModel.filteredDocuments) { document in
                        DocumentCard(document: document) { selectedDocument = document }
                    }
                }
            }
        }
    }
}
