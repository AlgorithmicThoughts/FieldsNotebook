namespace Accessors.Speech;

/// <summary>The speech service subscription. Supply the key through user-secrets or an environment variable.</summary>
public sealed class SpeechOptions
{
    public const string SectionName = "Speech";
    public string Key { get; set; } = "";
    public string Region { get; set; } = "";
    public string Language { get; set; } = "en-US";
}
