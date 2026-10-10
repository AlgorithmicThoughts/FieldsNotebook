using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DataContract.Delays;

public sealed class DelayReportRequest
{
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(4000)] public string Description { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int OwnerCrewId { get; set; }
    [MinLength(1)] public List<DelayReportImpact> Impacts { get; set; } = [];
    public int? BlockingWorkItemId { get; set; }
    [Range(1, int.MaxValue)] public int DelayTagId { get; set; }
    [Required] public string ReportType { get; set; } = string.Empty;
    // Retained to reject report requests that try to set the Superintendent's estimate.
    public DateTimeOffset? ExpectedReadyAt { get; set; }
    [MinLength(1), MaxLength(5)] public List<DelayReportPhoto> Photos { get; set; } = [];
    [Range(1, int.MaxValue)] public int SegmentId { get; set; }
    public int? SiteMapId { get; set; }
    [Range(0, 1)] public decimal? PinX { get; set; }
    [Range(0, 1)] public decimal? PinY { get; set; }
    public string? LocationSource { get; set; }
    [StringLength(200)] public string? LocationName { get; set; }
    public List<int> NotifyUserIds { get; set; } = [];
}

public sealed class DelayReportImpact
{
    [Range(1, int.MaxValue)] public int BlockedCrewId { get; set; }
    public int? BlockedWorkItemId { get; set; }
}

public sealed class DelayReportPhoto
{
    [Required] public IFormFile File { get; set; } = null!;
    [Required] public IFormFile Thumbnail { get; set; } = null!;
    [Range(-90, 90)] public decimal? Latitude { get; set; }
    [Range(-180, 180)] public decimal? Longitude { get; set; }
}
