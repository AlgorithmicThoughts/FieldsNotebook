using System.ComponentModel.DataAnnotations;

namespace DataContract.Reports;

/// <summary>
/// GET /api/reports/delays — the approved reporting contract, and the whole of it.
/// Both dates are inclusive and compare against the UTC day of Delays.CreatedAt; an omitted date leaves that end open.
/// A report without a project would expose every project's delays, so an empty scope fails rather than widening.
/// </summary>
public sealed record DelayReportRequest(
    [Required(ErrorMessage = "A delay report needs at least one project."),
     MinLength(1, ErrorMessage = "A delay report needs at least one project.")] int[]? ProjectIds,
    DateOnly? DateFrom,
    DateOnly? DateTo) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProjectIds is not null && ProjectIds.Any(id => id < 1))
        {
            yield return new ValidationResult("Project IDs must be greater than zero.", [nameof(ProjectIds)]);
        }

        if (DateFrom > DateTo)
        {
            yield return new ValidationResult("The start date must be on or before the end date.", [nameof(DateFrom), nameof(DateTo)]);
        }
    }
}
