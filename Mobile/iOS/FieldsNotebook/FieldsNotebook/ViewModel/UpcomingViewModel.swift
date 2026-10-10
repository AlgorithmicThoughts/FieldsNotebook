import Foundation
import Observation

@MainActor
@Observable
final class UpcomingViewModel {
    private(set) var work: [UpcomingWork] = []
    private(set) var lastChecked = Date()
    @ObservationIgnored private let service: any NotebookProviding
    private var project: HomeProject?

    init(service: any NotebookProviding) { self.service = service }

    func load(project: HomeProject) {
        self.project = project
        refresh()
    }

    func refresh() {
        guard let project else { return }
        work = service.upcoming(for: project)
        lastChecked = Date()
    }
}
