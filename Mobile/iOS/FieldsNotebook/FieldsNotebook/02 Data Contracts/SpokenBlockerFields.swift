import Foundation
import FoundationModels

/// Suggestions only; project IDs are resolved against the current form's options.
@Generable
nonisolated struct SpokenBlockerFields: Sendable {
    @Guide(description: "A factual title of at most six words, based only on the note")
    var title: String
    @Guide(description: "The blocker and work it stops in one or two sentences. Do not add facts.")
    var description: String
    @Guide(description: "The exact name of the crew responsible for fixing the blocker. Nil unless explicitly named as responsible in the note.")
    var responsibleCrew: String?
    @Guide(description: "The cause of the blocker. Nil if the note does not explain the cause.")
    var reason: SpokenBlockerReason?
}

@Generable
nonisolated enum SpokenBlockerReason: String, Sendable {
    case missingMaterial = "Missing material"
    case personnelShortage = "Personnel shortage"
    case qualityConcern = "Quality concern"
    case planConflict = "Plan conflict"
    case safetyIssue = "Safety issue"
    case pendingInspection = "Pending inspection"
    case equipmentBreakdown = "Equipment breakdown"
    case adverseWeather = "Adverse weather"
    case other = "Other"
}

nonisolated struct SpeechCrewOption: Sendable {
    let id: String
    let name: String
}

nonisolated struct SpokenBlockerSuggestions: Sendable {
    let fields: SpokenBlockerFields
    let usedAppleIntelligence: Bool
}

nonisolated enum BlockerSpeechError: LocalizedError {
    case microphoneDenied, speechUnavailable, recordingFailed, emptyTranscript
    var errorDescription: String? {
        switch self {
        case .microphoneDenied: "Microphone access is needed. Enable it for Field Notebook in Settings, or type your report."
        case .speechUnavailable: "Apple Speech is unavailable for this device or language. You can type your report."
        case .recordingFailed: "The recording could not be captured. Try again or type your report."
        case .emptyTranscript: "No speech was recognized. Try again or type your report."
        }
    }
}
