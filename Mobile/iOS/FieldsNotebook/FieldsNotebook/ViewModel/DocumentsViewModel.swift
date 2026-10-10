import Foundation
import Observation

@MainActor
@Observable
final class DocumentsViewModel {
    var query = ""
    var category: DocumentCategory = .all
    private(set) var documents: [SiteDocument] = []
    @ObservationIgnored private let service: any NotebookProviding
    private var project: HomeProject?

    init(service: any NotebookProviding) { self.service = service }

    var filteredDocuments: [SiteDocument] {
        documents.filter(matchesCategory).filter(matchesSearch)
    }

    func load(project: HomeProject) {
        self.project = project
        query = ""
        category = .all
        refresh()
    }

    func refresh() {
        guard let project else { return }
        documents = service.documents(for: project)
    }

    private func matchesCategory(_ document: SiteDocument) -> Bool {
        category == .all || category == document.category
    }

    private func matchesSearch(_ document: SiteDocument) -> Bool {
        let search = query.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !search.isEmpty else { return true }
        return (document.title + " " + document.note).localizedCaseInsensitiveContains(search)
    }
}
