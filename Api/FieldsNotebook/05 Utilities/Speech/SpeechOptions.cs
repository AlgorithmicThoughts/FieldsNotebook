namespace Utilities.Speech;

public sealed class SpeechOptions
{
    public const string SectionName = "Speech";
    public string Key { get; set; } = "";
    public string Region { get; set; } = "";
    public string Language { get; set; } = "en-US";
}
