namespace Accessors.Speech
{
    public sealed class SpeechOptions
    {
        public const string SectionName = "Speech";
        public string Key { get; set; } = "";
        public string Region { get; set; } = "";
        public string Language { get; set; } = "en-US";
    }

    /// <summary>The speech recognition service. It turns a recording into text and decides nothing about it.</summary>
    public interface ISpeechAccessor
    {
        /// <summary>The recognised phrases of a WAV recording, joined by spaces; empty when nothing was said.</summary>
        Task<string> TranscribeAsync(Stream audio, CancellationToken cancellationToken);
    }
}
