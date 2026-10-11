import Foundation

enum BlockerValidation {
    static func detailsIssues(_ draft: BlockerDraft, options: BlockerOptions, user: HomeUser) -> [String] {
        descriptionIssues(draft) + crewIssues(draft, options: options, user: user) + scopeIssues(draft, options: options)
    }

    private static func descriptionIssues(_ draft: BlockerDraft) -> [String] {
        [draft.description.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty ? "Describe the blocker and the work it stops." : nil,
         draft.title.count > 200 ? "Keep the title within 200 characters." : nil,
         draft.description.count > 4000 ? "Keep the description within 4,000 characters." : nil,
         DelayReasons.all.contains(draft.reason) ? nil : "Choose a reason."].compactMap { $0 }
    }

    private static func crewIssues(_ draft: BlockerDraft, options: BlockerOptions, user: HomeUser) -> [String] {
        let validIDs = Set(options.crews.map(\.id))
        return [validIDs.contains(draft.responsibleCrewID) ? nil : "Choose the responsible crew.",
                draft.affectedCrewIDs.isEmpty ? "Select at least one affected crew." : nil,
                draft.affectedCrewIDs.isSubset(of: validIDs) ? nil : "Choose affected crews from this project.",
                draft.affectedCrewIDs.contains(draft.responsibleCrewID) ? "The responsible crew cannot also be an affected crew." : nil]
            .compactMap { $0 } + authorityIssues(draft, options: options, user: user)
    }

    private static func authorityIssues(_ draft: BlockerDraft, options: BlockerOptions, user: HomeUser) -> [String] {
        let ownCrew = options.crews.first { $0.name == user.crew }?.id ?? ""
        let involved = draft.responsibleCrewID == ownCrew || draft.affectedCrewIDs.contains(ownCrew)
        return [user.role == "Foreman" ? nil : "Only the Foreman of an involved crew can report a blocker.",
                involved ? nil : "Your crew must be responsible or affected."].compactMap { $0 }
    }

    private static func scopeIssues(_ draft: BlockerDraft, options: BlockerOptions) -> [String] {
        let items = options.workItems.filter { draft.affectedCrewIDs.contains($0.crewID) && $0.segmentID == draft.segmentID }
        return [options.segments.contains { $0.id == draft.segmentID } ? nil : "Choose the work segment.",
                draft.workItemIDs.isSubset(of: Set(items.map(\.id))) ? nil : "Choose work items for the affected crews and selected segment."].compactMap { $0 }
    }

    static func evidenceIssues(_ draft: BlockerDraft) -> [String] {
        [draft.photos.isEmpty ? "Add at least one photo." : nil,
         draft.photos.count > 5 ? "Use no more than five photos." : nil,
         draft.pin == nil ? "Place a pin on the sample site map." : nil].compactMap { $0 } + locationIssues(draft) + pinIssues(draft)
    }

    private static func locationIssues(_ draft: BlockerDraft) -> [String] {
        let hasGPS = draft.photos.contains { $0.latitude != nil && $0.longitude != nil }
        let needsName = !hasGPS && draft.locationName.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
        return [needsName ? "Enter a location name when photo GPS is unavailable." : nil,
                draft.locationName.count > 200 ? "Keep the location name within 200 characters." : nil].compactMap { $0 }
    }

    private static func pinIssues(_ draft: BlockerDraft) -> [String] {
        guard let pin = draft.pin else { return [] }
        guard (0...1).contains(pin.x), (0...1).contains(pin.y) else { return ["Choose a pin within the sample site map."] }
        return []
    }

    static func allIssues(_ draft: BlockerDraft, options: BlockerOptions, user: HomeUser) -> [String] {
        detailsIssues(draft, options: options, user: user) + evidenceIssues(draft) + recipientIssues(draft, options: options)
    }

    private static func recipientIssues(_ draft: BlockerDraft, options: BlockerOptions) -> [String] {
        guard draft.additionalRecipientIDs.isSubset(of: Set(options.people.map(\.id))) else { return ["Choose people from this project."] }
        return []
    }
}
