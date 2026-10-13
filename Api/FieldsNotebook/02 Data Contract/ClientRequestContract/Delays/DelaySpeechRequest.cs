using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DataContract.Delays;

/// <summary>A spoken delay description, sent as multipart form data as a WAV recording.</summary>
public sealed class DelaySpeechRequest : IValidatableObject
{
    [Required]
    public IFormFile Audio { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Audio is { Length: 0 })
        {
            yield return new ValidationResult("Audio is required.", [nameof(Audio)]);
        }
    }
}
