namespace Accessors.Speech;

/// <summary>The speech recognition service. It turns a recording into text and decides nothing about it.</summary>
public interface ISpeechAccessor : IAccessor
{
    /// <summary>The recognised phrases of a WAV recording, joined by spaces; empty when nothing was said.</summary>
    Task<string> TranscribeAsync(Stream audio, CancellationToken cancellationToken);
}
