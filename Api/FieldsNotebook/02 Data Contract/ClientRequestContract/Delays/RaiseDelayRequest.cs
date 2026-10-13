using System.ComponentModel.DataAnnotations;
using Common.Delays;

namespace DataContract.Delays;

/// <summary>
/// A Foreman's new delay, sent as multipart form data. Rules that need the project's crews, tags
/// and work items belong to the report Engine; the rules here need only the request itself.
/// </summary>
public sealed class RaiseDelayRequest : IValidatableObject
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int OwnerCrewId { get; set; }

    public List<DelayReportImpact> Impacts { get; set; } = [];

    public int? BlockingWorkItemId { get; set; }

    [Range(1, int.MaxValue)]
    public int DelayTagId { get; set; }

    [Required, AllowedValues(DelayReportTypes.AfterArrival, DelayReportTypes.AdvanceNotice, ErrorMessage = "Choose a valid report type.")]
    public string ReportType { get; set; } = string.Empty;

    public List<DelayReportPhoto> Photos { get; set; } = [];

    [Range(1, int.MaxValue)]
    public int SegmentId { get; set; }

    public int? SiteMapId { get; set; }

    [Range(0, 1)]
    public decimal? PinX { get; set; }

    [Range(0, 1)]
    public decimal? PinY { get; set; }

    [StringLength(200)]
    public string? LocationName { get; set; }

    public List<int> NotifyUserIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PinX.HasValue != PinY.HasValue)
        {
            yield return new ValidationResult("Both pin coordinates are required.", [nameof(PinX), nameof(PinY)]);
        }

        if (PinX is null && string.IsNullOrWhiteSpace(LocationName) && Photos.All(p => p.Latitude is null))
        {
            yield return new ValidationResult("Place a site pin or enter a location name when photos have no GPS.",
                [nameof(PinX), nameof(LocationName)]);
        }
    }
}
