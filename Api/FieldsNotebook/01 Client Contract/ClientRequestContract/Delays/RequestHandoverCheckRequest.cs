namespace DataContract.Delays;

/// <summary>
/// The responsible crew submits the delay for review with the proof of work: one to five photos that show
/// the blocker is gone, sent as multipart form data in the same shape as the initial report's photos.
/// </summary>
public sealed class RequestHandoverCheckRequest
{
    public List<DelayReportPhoto> Photos { get; set; } = [];
}
