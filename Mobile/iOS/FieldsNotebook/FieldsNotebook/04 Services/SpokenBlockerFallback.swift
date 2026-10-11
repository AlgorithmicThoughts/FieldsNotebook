import Foundation

/// Conservative fallback when the system language model is unavailable or fails.
/// Only explicit assignments and recognizable causes are selected; ambiguity stays nil.
nonisolated enum SpokenBlockerFallback {
    static func fields(from transcript: String, crews: [SpeechCrewOption]) -> SpokenBlockerFields {
        let reason = recognizedReason(transcript)
        let title = reason?.rawValue ?? transcript.split(whereSeparator: \.isWhitespace).prefix(6).joined(separator: " ")
        return SpokenBlockerFields(title: title, description: transcript,
                                   responsibleCrew: responsibleCrew(transcript, crews: crews), reason: reason)
    }

    private static func responsibleCrew(_ transcript: String, crews: [SpeechCrewOption]) -> String? {
        let matches = crews.filter { crew in
            let name = NSRegularExpression.escapedPattern(for: crew.name)
            let assignment = #"\s+(?:(?:is|are)\s+responsible\b|(?:was|were)\s+supposed\s+to\s+(?:order|deliver|repair|fix|complete)\b|(?:needs|need|has|have)\s+to\s+(?:order|deliver|repair|fix|complete)\b)"#
            return contains(#"\b"# + name + #"\b"# + assignment, in: transcript)
        }
        return matches.count == 1 ? matches.first?.name : nil
    }

    private static func recognizedReason(_ transcript: String) -> SpokenBlockerReason? {
        let reasons = patterns.filter { contains($0.pattern, in: transcript) }.map(\.reason)
        return reasons.count == 1 ? reasons.first : nil
    }

    private static func contains(_ pattern: String, in text: String) -> Bool {
        guard let regex = try? NSRegularExpression(pattern: pattern, options: .caseInsensitive) else { return false }
        let range = NSRange(text.startIndex..., in: text)
        return regex.matches(in: text, range: range).contains { match in
            let prefix = (text as NSString).substring(to: match.range.location)
            return prefix.range(of: #"\b(?:no|not|without)\s*$"#, options: [.regularExpression, .caseInsensitive]) == nil
        }
    }

    private static let patterns: [(reason: SpokenBlockerReason, pattern: String)] = [
        (.missingMaterial, #"\bmissing materials?\b|\b(?:rebar|material|delivery|supplies)\b[^.!?]{0,60}\b(?:never arrived|hasn't arrived|not delivered|didn't arrive|missing)\b"#),
        (.personnelShortage, #"\b(?:personnel shortage|short staffed|not enough workers|staff shortage)\b"#),
        (.qualityConcern, #"\b(?:quality concern|failed quality check|defective material)\b"#),
        (.planConflict, #"\b(?:plan conflict|conflicting plans|drawing conflict)\b"#),
        (.safetyIssue, #"\b(?:safety issue|safety hazard|unsafe conditions)\b"#),
        (.pendingInspection, #"\b(?:pending inspection|waiting for inspection|awaiting inspection)\b"#),
        (.equipmentBreakdown, #"\b(?:equipment breakdown|broken equipment|crane broke|equipment failure)\b"#),
        (.adverseWeather, #"\b(?:adverse weather|severe weather|weather delay)\b"#),
        (.other, #"\bother reason\b"#)
    ]
}
