namespace DataContract.Home;

/// <summary>Normalized position from zero to one on the delay's site map image.</summary>
public sealed record DelayPinResponse(
    decimal X,
    decimal Y);
