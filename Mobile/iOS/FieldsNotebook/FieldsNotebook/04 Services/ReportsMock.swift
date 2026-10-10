import Foundation

struct ReportsMock: ReportsProviding {
    let projects: [HomeProject]
    private let notebookService: any NotebookProviding

    init(homeService: any HomeProviding, notebookService: any NotebookProviding) {
        projects = homeService.projects
        self.notebookService = notebookService
    }

    func report(for request: NotebookReportRequest) -> NotebookReport {
        let authorizedProjects = projects.filter { request.projectIds.contains($0.id) }
        return NotebookReport(request: request, records: authorizedProjects.flatMap { notebookService.delays(for: $0) })
    }

    func export(_ report: NotebookReport) throws -> Data {
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
        return try encoder.encode(SampleReportExport(report: report))
    }
}

/// A local sample file format, not a response contract or live integration.
private struct SampleReportExport: Encodable {
    let sampleData = true
    let request: NotebookReportRequest
    let records: [SampleReportRecord]

    init(report: NotebookReport) {
        request = report.request
        records = report.records.map { SampleReportRecord(record: $0) }
    }
}

private struct SampleReportRecord: Encodable {
    let delayId: String
    let projectId: String
    let description: String
    let reason: String
    let responsibleCrew: String
    let affectedCrews: [String]
    let status: String
    let highPriority: Bool
    let location: String
    let tentativeCompletion: String

    init(record: DelayRecord) {
        delayId = record.id
        projectId = record.project.id
        description = record.detail
        reason = record.summary.reason
        responsibleCrew = record.summary.responsibleCrew
        affectedCrews = record.summary.affectedCrews
        status = record.summary.status.rawValue
        highPriority = record.summary.isHighPriority
        location = record.summary.location
        tentativeCompletion = record.completion
    }
}
