namespace DataContract.Delays;

public sealed record DelaySpeechResponse(
    string Title, string Description, int? OwnerCrewId, int? DelayTagId);
