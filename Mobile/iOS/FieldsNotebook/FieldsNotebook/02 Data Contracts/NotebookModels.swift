import Foundation

struct DelayRecord: Identifiable {
    var id: String { summary.id }
    let summary: HomeDelay
    let project: HomeProject
    let detail: String
    let completion: String
}

enum TrackingScope: String, CaseIterable, Identifiable {
    case project = "This project"
    case assignedProjects = "All assigned projects"
    var id: Self { self }
}

enum TrackingFilter: String, CaseIterable, Identifiable {
    case all = "All delays"
    case blocking = "Blocking us"
    case waiting = "Waiting on us"
    case verification = "Ready to verify"
    case closed = "Closed"
    var id: Self { self }

    func matches(_ delay: HomeDelay, crew: String) -> Bool {
        if self == .all { return true }
        if self == .closed { return !delay.isActive }
        guard delay.isActive else { return false }
        return matchesCrew(delay, crew: crew)
    }

    private func matchesCrew(_ delay: HomeDelay, crew: String) -> Bool {
        if self == .waiting { return delay.responsibleCrew == crew }
        guard delay.affectedCrews.contains(crew) else { return false }
        if self == .verification { return delay.status == .verification }
        return self == .blocking
    }

    init(metric: HomeMetric?) {
        switch metric {
        case .blocking: self = .blocking
        case .dependent: self = .waiting
        case .verification: self = .verification
        case nil: self = .all
        }
    }
}

struct UpcomingWork: Identifiable {
    let id: String
    let title: String
    let crew: String
    let plannedTime: String
    let day: String
    let blockers: [DelayRecord]
    let completion: String
}

enum DocumentCategory: String, CaseIterable, Identifiable {
    case all = "All documents"
    case safety = "Safety"
    case schedule = "Schedule"
    case plans = "Plans"
    var id: Self { self }
}

struct DocumentSection: Identifiable {
    let id: String
    let title: String
    let detail: String
}

struct SiteDocument: Identifiable {
    let id: String
    let project: HomeProject
    let title: String
    let category: DocumentCategory
    let symbol: String
    let revision: String
    let note: String
    let sections: [DocumentSection]
    var pdfResourceName: String? = nil
}
