import Foundation

@MainActor
protocol ReportsProviding {
    var projects: [HomeProject] { get }
    func report(for request: NotebookReportRequest) -> NotebookReport
    func export(_ report: NotebookReport) throws -> Data
}
