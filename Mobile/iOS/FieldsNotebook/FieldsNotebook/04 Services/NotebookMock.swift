import Foundation

/// Project-scoped fixtures shared by Tracking, Documents, and Upcoming.
struct NotebookMock: NotebookProviding {
    private let homeService: any HomeProviding
    private let blockerService: (any ReportBlockerProviding)?

    init(homeService: any HomeProviding, blockerService: (any ReportBlockerProviding)? = nil) {
        self.homeService = homeService
        self.blockerService = blockerService
    }

    func delays(for project: HomeProject) -> [DelayRecord] {
        let snapshot = homeService.loadHome(for: project)
        var records = snapshot.delays.map { summary in
            if let submitted = blockerService?.records(for: project).first(where: { $0.id == summary.id }) { return submitted }
            return DelayRecord(summary: summary, project: project,
                        detail: Self.descriptions[summary.id] ?? summary.title,
                        completion: Self.completions[summary.id] ?? "Time not confirmed")
        }
        if project.id == "OF-017" {
            records.append(DelayRecord(
                summary: HomeDelay(id: "FN-1027", title: "Steel frame handover complete", reason: "Other",
                                   reasonSymbol: "checkmark.circle.fill", responsibleCrew: "Crew A", affectedCrews: ["Crew B"],
                                   location: "Bay 1 · Frame 06", status: .resolved),
                project: project, detail: "Crew A completed the fit-up. Crew B’s Foreman verified the handover, and the responsible Superintendent resolved the delay.",
                completion: "Resolved · 6 October, 1:12 PM"
            ))
        }
        return records
    }

    func allAssignedDelays() -> [DelayRecord] {
        homeService.projects.flatMap { delays(for: $0) }
    }

    func upcoming(for project: HomeProject) -> [UpcomingWork] {
        let snapshot = homeService.loadHome(for: project)
        let blockers = delays(for: project).filter { $0.summary.isActive && $0.summary.affectedCrews.contains(snapshot.user.crew) }
        guard !blockers.isEmpty else { return [] }
        return [UpcomingWork(
            id: project.id + "-next-work", title: Self.workTitles[project.id] ?? "Next crew activity",
            crew: snapshot.user.crew, plannedTime: project.id == "OF-017" ? "7:00 AM" : "8:00 AM",
            day: "Thursday, 8 October 2026", blockers: blockers,
            completion: snapshot.advanceNotice.map { $0.day + " · " + $0.time } ?? "Time not confirmed"
        )]
    }

    private static let descriptions = [
        "FN-1034": "Equipment in the east access lane prevents Crew A from moving parts into the fit-up area. This reopened delay requires a new acknowledgment and fresh handover checks.",
        "FN-1042": "Crew A needs more time to fit the steel parts for Frame 12. Crew B cannot start tomorrow’s weld at 7:00 AM. Completion is tentatively expected at 2:00 PM.",
        "FN-1040": "The overhead crane cannot lift the fitted frame into the weld bay. Every affected crew remains blocked until the lift is restored and the handover is verified.",
        "FN-1038": "The fit-up inspection is complete. Crew A reports that Frame 08 is ready for the affected crew’s Foreman to verify the handover.",
        "FN-1041": "Heavy rain prevents Crew A from completing the deck preparation. Crew B must wait for the completed and verified handover.",
        "FN-1039": "An equipment fault prevents Crew A from completing the material lift. Crew B cannot start until the lift and handover are complete."
    ]
    private static let completions = [
        "FN-1042": "Tomorrow · 8 October · 2:00 PM", "FN-1038": "Today · 7 October · 9:00 AM",
        "FN-1041": "Tomorrow · 8 October · 11:00 AM"
    ]
    private static let workTitles = [
        "OF-017": "Weld Frame 12", "WR-042": "Start deck work", "NC-063": "Start the material handover"
    ]
}
