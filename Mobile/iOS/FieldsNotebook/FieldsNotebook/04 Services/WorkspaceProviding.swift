import Foundation

@MainActor
protocol WorkspaceProviding {
    var projects: [HomeProject] { get }
    func preferences() -> WorkspacePreferences
    func savePreferences(_ preferences: WorkspacePreferences)
    func notices(for project: HomeProject) -> [NotebookNotice]
    func openNotice(id: String, project: HomeProject) -> DelayRecord?
}
