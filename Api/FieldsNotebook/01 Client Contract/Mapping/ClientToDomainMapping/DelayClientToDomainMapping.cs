using DataContract.Delays;
using DataContract.Domain.Delays;
using Microsoft.AspNetCore.Http;

namespace DataContract.Mapping.ClientToDomainMapping;

public static class DelayClientToDomainMapping
{
    public static RaiseDelayDraft ToDomain(this RaiseDelayRequest request)
    {
        return new RaiseDelayDraft(request.Title, request.Description, request.OwnerCrewId, request.DelayTagId, request.ReportType,
            request.BlockingWorkItemId, request.SegmentId, request.SiteMapId, request.PinX, request.PinY, request.LocationName,
            request.Impacts.ToDomain(), request.Photos.ToDomain(), request.NotifyUserIds.MapAll(id => id));
    }

    public static DelayAcknowledgmentChange ToDomain(this AcknowledgeDelayRequest request)
    {
        return new DelayAcknowledgmentChange(request.Choice, request.Note);
    }

    public static DelayVerificationChange ToDomain(this VerifyHandoverRequest request, int impactId)
    {
        return new DelayVerificationChange(impactId, request.Note);
    }

    public static DelayReturnChange ToDomain(this ReturnHandoverRequest request, int impactId)
    {
        return new DelayReturnChange(impactId, request.Reason.Trim());
    }

    /// <summary>A request-scoped stream source, so Managers never open HTTP upload types.</summary>
    public static DelaySpeechAudioRecord ToDomain(this DelaySpeechRequest request)
    {
        return new DelaySpeechAudioRecord(request.Audio.OpenReadStream);
    }

    public static IReadOnlyList<ReportImpactRecord> ToDomain(this IEnumerable<DelayReportImpact>? impacts)
    {
        return impacts.MapAll(i => new ReportImpactRecord(i.BlockedCrewId, i.BlockedWorkItemId));
    }

    /// <summary>
    /// Request-scoped stream sources with the metadata the photo rules check, so Engines and Accessors never see
    /// HTTP upload types. A missing file maps as empty and fails those rules.
    /// </summary>
    public static IReadOnlyList<ReportPhotoUploadRecord> ToDomain(this IEnumerable<DelayReportPhoto>? photos)
    {
        return photos.MapAll(p => new ReportPhotoUploadRecord(OpenOrEmpty(p.File), OpenOrEmpty(p.Thumbnail),
            p.File?.ContentType ?? string.Empty, p.File is null ? string.Empty : Path.GetFileName(p.File.FileName),
            p.Latitude, p.Longitude, p.File?.Length ?? 0, p.Thumbnail?.ContentType ?? string.Empty, p.Thumbnail?.Length ?? 0));
    }

    private static Func<Stream> OpenOrEmpty(IFormFile? file)
    {
        return file is null ? () => Stream.Null : file.OpenReadStream;
    }
}
