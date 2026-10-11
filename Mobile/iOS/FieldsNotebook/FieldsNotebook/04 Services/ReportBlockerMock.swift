import Foundation

/// Session-only drafts and submissions. No network requests or notification delivery.
@MainActor
final class ReportBlockerMock: ReportBlockerProviding {
    private var drafts: [String: BlockerDraft] = [:]
    private var submissions: [String: [DelayRecord]] = [:]
    private var submittedDrafts: [String: BlockerDraft] = [:]
    private var nextNumber = 1050

    func options(for project: HomeProject) -> BlockerOptions { BlockerFixtures.options(for: project) }
    func draft(for project: HomeProject, user: HomeUser) -> BlockerDraft? { drafts[key(project, user)] }
    func saveDraft(_ draft: BlockerDraft, project: HomeProject, user: HomeUser) { drafts[key(project, user)] = draft }
    func discardDraft(project: HomeProject, user: HomeUser) { drafts.removeValue(forKey: key(project, user)) }
    func records(for project: HomeProject) -> [DelayRecord] { submissions[project.id] ?? [] }

    func submit(_ draft: BlockerDraft, project: HomeProject, user: HomeUser) throws -> DelayRecord {
        let options = options(for: project)
        let issues = BlockerValidation.allIssues(draft, options: options, user: user)
        guard issues.isEmpty else { throw BlockerFormError.invalid(issues.joined(separator: "\n")) }
        let record = makeRecord(draft, project: project, options: options)
        submissions[project.id, default: []].insert(record, at: 0)
        submittedDrafts[record.id] = draft
        drafts.removeValue(forKey: key(project, user))
        nextNumber += 1
        return record
    }

    private func key(_ project: HomeProject, _ user: HomeUser) -> String { project.id + "|" + user.name + "|" + user.crew }

    private func makeRecord(_ draft: BlockerDraft, project: HomeProject, options: BlockerOptions) -> DelayRecord {
        let affected = options.crews.filter { draft.affectedCrewIDs.contains($0.id) }.map(\.name)
        let owner = options.crews.first { $0.id == draft.responsibleCrewID }?.name ?? ""
        let text = draft.description.trimmingCharacters(in: .whitespacesAndNewlines)
        let summary = HomeDelay(id: "FN-\(nextNumber)", title: reportTitle(draft, fallback: text), reason: draft.reason,
                                reasonSymbol: BlockerFixtures.reasonSymbol(draft.reason), responsibleCrew: owner,
                                affectedCrews: affected, location: draft.locationName, status: .blocked)
        let completion = draft.advanceNotice ? "Tentative · " + Self.dateLabel(draft.tentativeCompletion) : "Time not confirmed"
        return DelayRecord(summary: summary, project: project, detail: text, completion: completion)
    }

    private func reportTitle(_ draft: BlockerDraft, fallback: String) -> String {
        let title = draft.title.trimmingCharacters(in: .whitespacesAndNewlines)
        return String((title.isEmpty ? fallback : title).prefix(200))
    }

    static func dateLabel(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.timeZone = TimeZone(identifier: "America/Chicago")
        formatter.dateStyle = .medium
        formatter.timeStyle = .short
        return formatter.string(from: date) + " · Central"
    }
}
