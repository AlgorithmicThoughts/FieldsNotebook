using System.Text.RegularExpressions;
using DataContract.Delays;
using DataContract.Domain.Delays;

namespace Engines.Delays;

/// <summary>Suggests editable report fields from a transcript, using the project's live crews and reasons.</summary>
public static class DelaySpeechEngine
{
    private static readonly IReadOnlyDictionary<string, string[]> TagKeywords = new Dictionary<string, string[]>
    {
        ["MissingMaterial"] = ["material", "materials", "material shortage", "materials have not arrived", "materials haven't arrived", "out of stock", "waiting for material", "waiting for materials", "delivery", "supplies"],
        ["SafetyIssue"] = ["safety", "hazard", "hazardous", "unsafe", "danger", "dangerous"],
        ["PersonnelShortage"] = ["personnel", "staff shortage", "short staffed", "understaffed", "manpower", "not enough workers", "not enough people"],
        ["QualityConcern"] = ["quality", "defect", "defective", "rework", "damaged", "poor workmanship"],
        ["PlanConflict"] = ["plan conflict", "drawing", "drawings", "design", "blueprint", "plans conflict"],
        ["PendingInspection"] = ["inspection", "inspections", "inspect", "inspecting", "inspector"],
        ["EquipmentBreakdown"] = ["equipment", "breakdown", "broken", "repair", "repairs", "maintenance", "machine", "machinery"],
        ["AdverseWeather"] = ["weather", "rain", "raining", "storm", "snow", "wind", "lightning", "flood", "flooding"]
    };

    public static DelaySpeechResponse MapFields(string transcript, ReportChoicesRecord choices) =>
        new(MapTitle(transcript), transcript, MapCrew(transcript, choices.Crews), MapTag(transcript, choices.Tags));

    #region Private Methods

    private static string MapTitle(string transcript)
    {
        var explicitTitle = Regex.Match(transcript, @"\btitle\s+(?:is|should be)\s+(?<title>.+)", RegexOptions.IgnoreCase);
        var source = explicitTitle.Success ? explicitTitle.Groups["title"].Value : transcript;
        source = Regex.Replace(source, @"^\s*(?:the issue is|the problem is|there is|there are|we have)\s+", "", RegexOptions.IgnoreCase);
        var sentence = Regex.Split(source.Trim(), @"[.!?](?:\s|$)")[0];
        var words = Regex.Split(sentence.Trim(), @"\s+").Take(8);
        var title = string.Join(" ", words).Trim(' ', ',', ';', ':');
        return title.Length <= 80 ? title : title[..80].TrimEnd();
    }

    private static int? MapCrew(string transcript, IReadOnlyList<ReportCrewRecord> crews)
    {
        // Prefer a spoken assignment over mentions of affected crews elsewhere in the description.
        var assignment = Regex.Match(transcript,
            @"\b(?:responsible crew|responsible team|owner crew)\s*(?:is|should be|:)\s*(?<crew>[^.!?;]+)",
            RegexOptions.IgnoreCase);
        var source = assignment.Success ? "crew " + assignment.Groups["crew"].Value : transcript;
        var matches = crews.Select(crew => new { crew.Id, Score = CrewScore(source, crew.Name) })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ToArray();
        if (matches.Length == 0) return null;
        return matches.Count(match => match.Score == matches[0].Score) == 1 ? matches[0].Id : null;
    }

    private static int CrewScore(string source, string name)
    {
        // Derive spoken names from live crew data, e.g. 'welding' from 'Welding and Erection'.
        var normalizedSource = NormalizeCrewName(source);
        var normalizedName = NormalizeCrewName(name);
        if (ContainsPhrase(normalizedSource, normalizedName)) return 100;
        var words = normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Except(["crew", "team", "and", "the", "of"], StringComparer.OrdinalIgnoreCase)
            .Where(word => word.Length > 1);
        return words.Count(word => ContainsPhrase(normalizedSource, word));
    }

    private static string NormalizeCrewName(string value) =>
        Regex.Replace(value, @"[^\p{L}\p{N}]+", " ").Trim();

    private static int? MapTag(string transcript, IReadOnlyList<ReportTagRecord> tags)
    {
        var matches = tags
            .Select(tag => new { tag.Id, Score = TagScore(transcript, tag) })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ToArray();
        return matches.Length > 0 ? matches[0].Id : tags.FirstOrDefault(tag => tag.Code == "Other")?.Id;
    }

    private static int TagScore(string transcript, ReportTagRecord tag)
    {
        var keywords = TagKeywords.GetValueOrDefault(tag.Code, []);
        return keywords.Count(keyword => ContainsPhrase(transcript, keyword))
            + (ContainsPhrase(transcript, tag.Name) ? 2 : 0);
    }

    private static bool ContainsPhrase(string source, string phrase) =>
        Regex.IsMatch(source, $@"\b{Regex.Escape(phrase)}\b", RegexOptions.IgnoreCase);

    #endregion
}
