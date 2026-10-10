import Foundation
import Observation

@MainActor
@Observable
final class HomeViewModel {
    private(set) var snapshot: HomeSnapshot
    private(set) var lastChecked: Date

    @ObservationIgnored private let service: any HomeProviding

    var projects: [HomeProject] { service.projects }

    init(service: any HomeProviding) {
        self.service = service
        snapshot = service.loadHome(for: service.projects[0])
        lastChecked = Date()
    }

    func selectProject(_ project: HomeProject) {
        snapshot = service.loadHome(for: project)
        lastChecked = Date()
    }

    func refresh() {
        snapshot = service.loadHome(for: snapshot.project)
        lastChecked = Date()
    }
}
