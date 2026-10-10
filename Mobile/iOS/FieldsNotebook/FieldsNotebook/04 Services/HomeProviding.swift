import Foundation

@MainActor
protocol HomeProviding {
    var projects: [HomeProject] { get }
    func loadHome(for project: HomeProject) -> HomeSnapshot
}
