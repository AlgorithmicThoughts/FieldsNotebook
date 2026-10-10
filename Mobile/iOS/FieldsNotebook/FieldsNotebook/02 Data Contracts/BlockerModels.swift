import Foundation

// Local form models. These are not delay API request contracts.
enum BlockerStep: Int, CaseIterable, Identifiable {
    case details, evidence, review
    var id: Self { self }
    var title: String { ["Details", "Evidence & location", "Review"][rawValue] }
}

struct BlockerCrew: Identifiable {
    let id: String
    let name: String
    let superintendent: BlockerPerson
}

struct BlockerPerson: Identifiable {
    let id: String
    let name: String
    let detail: String
}

struct BlockerSegment: Identifiable {
    let id: String
    let name: String
}

struct BlockerWorkItem: Identifiable {
    let id: String
    let title: String
    let crewID: String
    let segmentID: String
}

struct BlockerOptions {
    let crews: [BlockerCrew]
    let segments: [BlockerSegment]
    let workItems: [BlockerWorkItem]
    let people: [BlockerPerson]
}

nonisolated struct BlockerPhoto: Identifiable, Sendable {
    let id: UUID
    let originalData: Data
    let thumbnailData: Data
    let latitude: Double?
    let longitude: Double?
}

struct BlockerPin: Equatable {
    let x: Double
    let y: Double
}

struct BlockerDraft {
    var description = ""
    var responsibleCrewID = ""
    var affectedCrewIDs: Set<String> = []
    var workItemIDs: Set<String> = []
    var segmentID = ""
    var reason = ""
    var advanceNotice = false
    var tentativeCompletion = Date()
    var photos: [BlockerPhoto] = []
    var pin: BlockerPin?
    var locationName = ""
    // Additional recipients belong to this draft, never to saved channel preferences.
    var additionalRecipientIDs: Set<String> = []
}

enum BlockerRecordingState { case idle, recording, ready }

nonisolated enum BlockerFormError: LocalizedError {
    case invalid(String)
    var errorDescription: String? {
        switch self { case .invalid(let message): message }
    }
}
