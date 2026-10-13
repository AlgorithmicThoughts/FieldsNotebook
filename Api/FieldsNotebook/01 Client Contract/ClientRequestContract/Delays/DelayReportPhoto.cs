using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DataContract.Delays;

public sealed class DelayReportPhoto : IValidatableObject
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [Required]
    public IFormFile Thumbnail { get; set; } = null!;

    [Range(-90, 90)]
    public decimal? Latitude { get; set; }

    [Range(-180, 180)]
    public decimal? Longitude { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Latitude.HasValue != Longitude.HasValue)
        {
            yield return new ValidationResult("Photo GPS requires both latitude and longitude.",
                [nameof(Latitude), nameof(Longitude)]);
        }
    }
}
