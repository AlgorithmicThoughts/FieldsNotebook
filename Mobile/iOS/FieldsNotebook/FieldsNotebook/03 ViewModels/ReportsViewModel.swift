import Foundation
import Observation

@MainActor
@Observable
final class ReportsViewModel {
    var selectedProjectIDs: Set<String> = []
    private(set) var report: NotebookReport?
    private(set) var errorMessage: String?
    var exportMessage: String?
    @ObservationIgnored private let service: any ReportsProviding
    private var workspaceProjectID: String?

    init(service: any ReportsProviding) { self.service = service }
    var projects: [HomeProject] { service.projects }
    var hasUnappliedChanges: Bool { selectedProjectIDs != Set(report?.request.projectIds ?? []) }

    func load(project: HomeProject) {
        guard workspaceProjectID != project.id else { return }
        workspaceProjectID = project.id
        selectedProjectIDs = [project.id]
        applyFilters()
    }

    func select(_ project: HomeProject, included: Bool) {
        if included { selectedProjectIDs.insert(project.id) }
        else { selectedProjectIDs.remove(project.id) }
        errorMessage = nil
        exportMessage = nil
    }

    func applyFilters() {
        guard !selectedProjectIDs.isEmpty else {
            errorMessage = "Choose at least one project."
            return
        }
        errorMessage = nil
        exportMessage = nil
        report = service.report(for: NotebookReportRequest(projectIds: selectedProjectIDs.sorted()))
    }

    func exportData() -> Data? {
        guard !hasUnappliedChanges else {
            errorMessage = "Apply your project selection before exporting."
            return nil
        }
        guard let report else { return nil }
        do {
            errorMessage = nil
            return try service.export(report)
        } catch {
            errorMessage = "Could not prepare the report: \(error.localizedDescription)"
            return nil
        }
    }
}
