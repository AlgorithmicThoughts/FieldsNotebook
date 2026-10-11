using Microsoft.AspNetCore.Http;

namespace DataContract.Delays;

public sealed class DelaySpeechRequest
{
    public IFormFile Audio { get; set; } = null!;
}
