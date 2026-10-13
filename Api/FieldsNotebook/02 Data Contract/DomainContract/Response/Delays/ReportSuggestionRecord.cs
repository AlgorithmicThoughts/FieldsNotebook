namespace DataContract.Domain.Delays;

/// <summary>Editable report fields suggested from a transcript. Nothing is saved.</summary>
public sealed record ReportSuggestionRecord(
    string Title,
    string Description,
    int? OwnerCrewId,
    int? DelayTagId);
