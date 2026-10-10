import Foundation
import Observation

@MainActor
@Observable
final class NotificationsViewModel {
    var filter: NoticeFilter = .all
    private(set) var notices: [NotebookNotice] = []
    private(set) var lastChecked = Date()
    private(set) var errorMessage: String?
    @ObservationIgnored private let service: any WorkspaceProviding
    private var project: HomeProject?

    init(service: any WorkspaceProviding) { self.service = service }

    var unreadCount: Int { notices.filter(\.isUnread).count }
    var visibleNotices: [NotebookNotice] {
        guard filter == .unread else { return notices }
        return notices.filter(\.isUnread)
    }

    func load(project: HomeProject) {
        if self.project?.id != project.id { filter = .all }
        self.project = project
        errorMessage = nil
        refresh()
    }

    func refresh() {
        guard let project else { return }
        notices = service.notices(for: project)
        lastChecked = Date()
    }

    func open(_ notice: NotebookNotice) -> DelayRecord? {
        guard let project, let record = service.openNotice(id: notice.id, project: project) else {
            errorMessage = "This notice is no longer available in the selected project. Refresh and try again."
            return nil
        }
        errorMessage = nil
        refresh()
        return record
    }
}
