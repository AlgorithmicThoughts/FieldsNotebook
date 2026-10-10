import Foundation

/// The approved report request shape. Dates are omitted for an all-dates request.
struct NotebookReportRequest: Encodable {
    let projectIds: [String]
    var dateFrom: String?
    var dateTo: String?
}

struct NotebookReport {
    let request: NotebookReportRequest
    let records: [DelayRecord]

    var reasonCounts: [DelayReasonCount] {
        DelayReasons.all.map { reason in
            DelayReasonCount(reason: reason, count: records.filter { $0.summary.reason == reason }.count)
        }
    }

    var activeCount: Int { records.filter { $0.summary.isActive }.count }
    var highPriorityCount: Int { records.filter { $0.summary.isHighPriority }.count }
}
