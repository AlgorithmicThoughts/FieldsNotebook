import Foundation

@MainActor
protocol ReportBlockerProviding {
    func options(for project: HomeProject) -> BlockerOptions
    func draft(for project: HomeProject, user: HomeUser) -> BlockerDraft?
    func saveDraft(_ draft: BlockerDraft, project: HomeProject, user: HomeUser)
    func discardDraft(project: HomeProject, user: HomeUser)
    func submit(_ draft: BlockerDraft, project: HomeProject, user: HomeUser) throws -> DelayRecord
    func records(for project: HomeProject) -> [DelayRecord]
}
