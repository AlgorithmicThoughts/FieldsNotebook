namespace DataContract.Delays;

/// <summary>
/// Proof-of-work photos that show the blocker is gone. Every resolution carries them, sent as
/// multipart form data in the same shape as the initial report's photos.
/// </summary>
public sealed class ResolveDelayRequest
{
    public List<DelayReportPhoto> Photos { get; set; } = [];
}
