using DataContract.Delays;
using DataContract.Domain.Delays;

namespace DataContract.Mapping.ClientToDomainMapping;

public static class DelayClientToDomainMapping
{
    public static DelayAcknowledgmentChange ToDomain(this AcknowledgeDelayRequest request)
    {
        return new DelayAcknowledgmentChange(request.Choice, request.Note);
    }

    public static DelayVerificationChange ToDomain(this VerifyHandoverRequest request, int impactId)
    {
        return new DelayVerificationChange(impactId, request.Note);
    }

    public static IReadOnlyList<ReportImpactRecord> ToDomain(this IEnumerable<DelayReportImpact>? impacts)
    {
        return impacts.MapAll(i => new ReportImpactRecord(i.BlockedCrewId, i.BlockedWorkItemId));
    }

    /// <summary>Request-scoped stream sources, so Accessors never see HTTP upload types.</summary>
    public static IReadOnlyList<ReportPhotoUploadRecord> ToDomain(this IEnumerable<DelayReportPhoto>? photos)
    {
        return photos.MapAll(p => new ReportPhotoUploadRecord(p.File.OpenReadStream, p.Thumbnail.OpenReadStream,
            p.File.ContentType, Path.GetFileName(p.File.FileName), p.Latitude, p.Longitude));
    }
}
