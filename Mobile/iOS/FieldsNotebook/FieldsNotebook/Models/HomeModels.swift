import Foundation

struct HomeProject: Identifiable, Hashable {
    let id: String
    let name: String
    let place: String
}

struct HomeUser {
    let name: String
    let initials: String
    let crew: String
    let role: String
}

// Presentation states for the mock dashboard, not API status codes.
enum HomeDelayStatus: String {
    case blocked = "Blocked"
    case inProgress = "In progress"
    case verification = "Ready to verify"
    case resolved = "Resolved"

    var symbol: String {
        switch self {
        case .blocked: "pause.circle"
        case .inProgress: "wrench.and.screwdriver"
        case .verification: "doc.text.magnifyingglass"
        case .resolved: "checkmark.circle.fill"
        }
    }
}

struct HomeDelay: Identifiable {
    let id: String
    let title: String
    let reason: String
    let reasonSymbol: String
    let responsibleCrew: String
    let affectedCrews: [String]
    let location: String
    let status: HomeDelayStatus
    var isHighPriority = false

    var isActive: Bool { status != .resolved }

    var crewSummary: String {
        "\(responsibleCrew) → \(affectedCrews.joined(separator: ", "))"
    }
}

struct HomeAdvanceNotice {
    let delayID: String
    let headline: String
    let detail: String
    let time: String
    let day: String
}

struct HomeActivity: Identifiable {
    let id: String
    let title: String
    let detail: String
    let time: String
    let delayID: String
}

enum HomeMetric: String, CaseIterable, Identifiable {
    case blocking = "Blocking our crew"
    case dependent = "Crews waiting on us"
    case verification = "Ready to verify"

    var id: Self { self }

    var symbol: String {
        switch self {
        case .blocking: "pause.circle"
        case .dependent: "person.2"
        case .verification: "doc.text.magnifyingglass"
        }
    }
}

struct HomeSnapshot {
    let project: HomeProject
    let user: HomeUser
    let dateLabel: String
    let advanceNotice: HomeAdvanceNotice?
    let delays: [HomeDelay]
    let activities: [HomeActivity]
    let unreadNotificationCount: Int

    func count(for metric: HomeMetric) -> Int {
        switch metric {
        case .blocking:
            delays.filter { $0.affectedCrews.contains(user.crew) }.count
        case .dependent:
            Set(delays.filter { $0.responsibleCrew == user.crew }.flatMap(\.affectedCrews)).count
        case .verification:
            delays.filter { $0.affectedCrews.contains(user.crew) && $0.status == .verification }.count
        }
    }
}
