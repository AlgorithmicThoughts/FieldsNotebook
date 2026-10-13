namespace DataContract.Domain.Delays;

/// <summary>A spoken delay description as a request-scoped WAV stream source.</summary>
public sealed record DelaySpeechAudioRecord(Func<Stream> OpenAudio);
