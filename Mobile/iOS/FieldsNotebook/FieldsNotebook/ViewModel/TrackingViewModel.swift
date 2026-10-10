import Foundation
import Observation

@MainActor
@Observable
final class TrackingViewModel {
    var query = ""
    var scope: TrackingScope = .project
    var filter: TrackingFilter = .all
    private(set) var records: [DelayRecord] = []
    private(set) var lastChecked = Date()
    @ObservationIgnored private let service: any NotebookProviding
    private var projectID = ""
    private var crew = ""

    init(service: any NotebookProviding) { self.service = service }

    var filteredRecords: [DelayRecord] {
        scopedRecords.filter { filter.matches($0.summary, crew: crew) }.filter(matchesSearch)
    }

    private var scopedRecords: [DelayRecord] {
        guard scope == .project else { return records }
        return records.filter { $0.project.id == projectID }
    }

    func load(project: HomeProject, crew: String, initialFilter: TrackingFilter) {
        projectID = project.id
        self.crew = crew
        filter = initialFilter
        refresh()
    }

    func refresh() {
        records = service.allAssignedDelays()
        lastChecked = Date()
    }

    func clearFilters() {
        query = ""
        filter = .all
    }

    private func matchesSearch(_ record: DelayRecord) -> Bool {
        let search = query.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !search.isEmpty else { return true }
        let text = [record.id, record.summary.title, record.summary.reason, record.summary.location,
                    record.summary.crewSummary, record.project.name].joined(separator: " ")
        return text.localizedCaseInsensitiveContains(search)
    }
}
