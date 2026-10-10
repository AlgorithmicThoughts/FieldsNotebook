import Foundation

@MainActor
protocol NotebookProviding {
    func delays(for project: HomeProject) -> [DelayRecord]
    func allAssignedDelays() -> [DelayRecord]
    func documents(for project: HomeProject) -> [SiteDocument]
    func upcoming(for project: HomeProject) -> [UpcomingWork]
}
