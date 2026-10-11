import Foundation
import Observation

@MainActor
@Observable
final class ReportBlockerViewModel {
    let project: HomeProject
    let user: HomeUser
    let options: BlockerOptions
    let recording: BlockerRecordingViewModel
    var draft: BlockerDraft
    private(set) var step: BlockerStep = .details
    private(set) var issues: [String] = []
    private(set) var isLoadingPhotos = false
    private(set) var submittedRecord: DelayRecord?
    var statusMessage: String?
    @ObservationIgnored private let service: any ReportBlockerProviding
    @ObservationIgnored private let photoLoader: BlockerPhotoLoader

    init(project: HomeProject, user: HomeUser, service: any ReportBlockerProviding, photoLoader: BlockerPhotoLoader, recording: BlockerRecordingViewModel) {
        self.project = project
        self.user = user
        self.service = service
        self.photoLoader = photoLoader
        self.recording = recording
        options = service.options(for: project)
        draft = service.draft(for: project, user: user) ?? BlockerDraft()
        setInitialCrews()
    }

    var responsibleCrew: BlockerCrew? { options.crews.first { $0.id == draft.responsibleCrewID } }
    var affectedCrews: [BlockerCrew] { options.crews.filter { draft.affectedCrewIDs.contains($0.id) } }
    var segmentName: String { options.segments.first { $0.id == draft.segmentID }?.name ?? "Not selected" }
    var selectedWorkItems: [BlockerWorkItem] { options.workItems.filter { draft.workItemIDs.contains($0.id) } }
    var optionalPeople: [BlockerPerson] { options.people.filter { $0.id != responsibleCrew?.superintendent.id } }
    var selectedPeople: [BlockerPerson] { optionalPeople.filter { draft.additionalRecipientIDs.contains($0.id) } }

    func workItems(for crew: BlockerCrew) -> [BlockerWorkItem] {
        options.workItems.filter { $0.crewID == crew.id && $0.segmentID == draft.segmentID }
    }

    func setAffected(_ crew: BlockerCrew, included: Bool) {
        if included { draft.affectedCrewIDs.insert(crew.id) }
        else { draft.affectedCrewIDs.remove(crew.id) }
        reconcileWorkItems()
    }

    func reconcileWorkItems() {
        let valid = options.workItems.filter { draft.affectedCrewIDs.contains($0.crewID) && $0.segmentID == draft.segmentID }
        draft.workItemIDs.formIntersection(Set(valid.map(\.id)))
    }

    func next() {
        issues = step == .details ? BlockerValidation.detailsIssues(draft, options: options, user: user) : BlockerValidation.evidenceIssues(draft)
        guard issues.isEmpty else { return }
        step = step == .details ? .evidence : .review
    }

    func back() {
        issues = []
        step = step == .review ? .evidence : .details
    }

    func sendRecording() {
        let baseline = draft
        let crews = options.crews.map { SpeechCrewOption(id: $0.id, name: $0.name) }
        recording.send(crews: crews) { [weak self] transcript, fields in
            self?.applySpeech(transcript, fields: fields, baseline: baseline)
        }
    }

    private func applySpeech(_ transcript: String, fields: SpokenBlockerFields?, baseline: BlockerDraft) {
        let suggested = fields?.description.trimmingCharacters(in: .whitespacesAndNewlines) ?? ""
        let description = suggested.isEmpty ? transcript : suggested
        draft.description = keepingEdits(draft.description, baseline: baseline.description, suggestion: String(description.prefix(4000)))
        guard let fields else { return }
        draft.title = keepingEdits(draft.title, baseline: baseline.title, suggestion: String(fields.title.prefix(200)))
        applySpeechSelections(fields, baseline: baseline)
    }

    private func keepingEdits(_ current: String, baseline: String, suggestion: String) -> String {
        current == baseline ? suggestion : current
    }

    private func applySpeechSelections(_ fields: SpokenBlockerFields, baseline: BlockerDraft) {
        if draft.responsibleCrewID == baseline.responsibleCrewID,
           let crew = options.crews.first(where: { $0.name == fields.responsibleCrew }) {
            draft.responsibleCrewID = crew.id
        }
        if draft.reason == baseline.reason, let reason = fields.reason { draft.reason = reason.rawValue }
    }

    func saveDraft() {
        service.saveDraft(draft, project: project, user: user)
    }

    func discardDraft() { service.discardDraft(project: project, user: user) }

    func submit() {
        guard submittedRecord == nil else { return }
        do {
            submittedRecord = try service.submit(draft, project: project, user: user)
            issues = []
        } catch { issues = [error.localizedDescription] }
    }

    func addPhotos(_ dataItems: [Data]) async {
        await importPhotos { dataItems }
    }

    func importPhotos(using load: @MainActor () async throws -> [Data]) async {
        isLoadingPhotos = true
        defer { isLoadingPhotos = false }
        do {
            let dataItems = try await load()
            for data in dataItems { try await appendPhoto(data) }
            statusMessage = nil
        } catch is CancellationError {
            return
        } catch { statusMessage = error.localizedDescription }
    }

    private func appendPhoto(_ data: Data) async throws {
        guard draft.photos.count < 5 else { throw BlockerFormError.invalid("Use no more than five photos. Remove one before adding another.") }
        let photo = try await photoLoader.photo(from: data)
        try Task.checkCancellation()
        draft.photos.append(photo)
    }

    private func setInitialCrews() {
        guard draft.responsibleCrewID.isEmpty else { return }
        draft.responsibleCrewID = options.crews.first { $0.name != user.crew }?.id ?? ""
        if let ownCrew = options.crews.first(where: { $0.name == user.crew }) { draft.affectedCrewIDs = [ownCrew.id] }
    }
}
