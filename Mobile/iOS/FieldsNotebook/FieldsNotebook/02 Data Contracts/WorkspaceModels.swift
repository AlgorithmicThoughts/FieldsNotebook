import Foundation

struct WorkspacePreferences {
    var emailEnabled = true
    var smsEnabled = false
}

/// Presentation fixture for the signed-in demo recipient, not a wire contract.
struct NotebookNotice: Identifiable {
    let id: String
    let record: DelayRecord
    let title: String
    let timeLabel: String
    var readAt: Date?
    var isUnread: Bool { readAt == nil }
}

enum NoticeFilter: String, CaseIterable, Identifiable {
    case all = "All notices"
    case unread = "Unread"
    var id: Self { self }
}

struct DelayReasonCount: Identifiable {
    var id: String { reason }
    let reason: String
    let count: Int
}

enum DelayReasons {
    static let all = ["Missing material", "Safety issue", "Personnel shortage", "Quality concern",
                      "Plan conflict", "Pending inspection", "Equipment breakdown", "Adverse weather", "Other"]
}
