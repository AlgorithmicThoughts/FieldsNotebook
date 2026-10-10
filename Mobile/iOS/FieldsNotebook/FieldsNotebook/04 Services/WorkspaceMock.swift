import Foundation

/// Session-only preferences and notices for the admin demo account (John Davis).
/// Opening a notice changes only its read state; no delay workflow or delivery occurs.
@MainActor
final class WorkspaceMock: WorkspaceProviding {
    let projects: [HomeProject]
    private var storedPreferences = WorkspacePreferences()
    private var storedNotices: [NotebookNotice]

    init(homeService: any HomeProviding, notebookService: any NotebookProviding) {
        projects = homeService.projects
        storedNotices = projects.flatMap { Self.fixtureNotices(project: $0, service: notebookService) }
    }

    func preferences() -> WorkspacePreferences { storedPreferences }
    func savePreferences(_ preferences: WorkspacePreferences) { storedPreferences = preferences }

    func notices(for project: HomeProject) -> [NotebookNotice] {
        storedNotices.filter { $0.record.project.id == project.id }
    }

    func openNotice(id: String, project: HomeProject) -> DelayRecord? {
        guard let index = storedNotices.firstIndex(where: { $0.id == id && $0.record.project.id == project.id }) else { return nil }
        if storedNotices[index].isUnread { storedNotices[index].readAt = Date() }
        return storedNotices[index].record
    }

    private static func fixtureNotices(project: HomeProject, service: any NotebookProviding) -> [NotebookNotice] {
        let records = service.delays(for: project)
        return noticeOrder.compactMap { id in
            guard let record = records.first(where: { $0.id == id }), let content = noticeContent[id] else { return nil }
            return NotebookNotice(id: "admin-" + record.id, record: record, title: content.0,
                                  timeLabel: content.1, readAt: record.id == "FN-1042" ? Date(timeIntervalSince1970: 1791379320) : nil)
        }
    }

    private static let noticeOrder = ["FN-1034", "FN-1038", "FN-1042", "FN-1041", "FN-1039"]
    private static let noticeContent: [String: (String, String)] = [
        "FN-1034": ("Access delay reopened. Your Superintendent will coordinate a new acknowledgment.", "7 Oct · 8:35 AM"),
        "FN-1038": ("Frame 08 is ready for your handover check.", "7 Oct · 8:30 AM"),
        "FN-1042": ("Tomorrow’s weld is delayed. Crew A estimates completion at 2:00 PM.", "7 Oct · 8:22 AM"),
        "FN-1041": ("Rain delays tomorrow’s deck work. Review the tentative handover.", "7 Oct · 8:00 AM"),
        "FN-1039": ("An equipment fault blocks your crew’s material handover.", "7 Oct · 7:30 AM")
    ]
}
