using System.Collections.Concurrent;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Utilities.Speech;

public static class SpeechRecognitionUtility
{
    public static async Task<string> RecognizeAsync(
        Stream audio, SpeechOptions settings, CancellationToken cancellationToken)
    {
        var filePath = Path.GetTempFileName();
        try
        {
            await using (var file = File.Create(filePath))
                await audio.CopyToAsync(file, cancellationToken);

            return await RecognizeFileAsync(filePath, settings, cancellationToken);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static async Task<string> RecognizeFileAsync(
        string filePath, SpeechOptions settings, CancellationToken cancellationToken)
    {
        var config = SpeechConfig.FromSubscription(settings.Key, settings.Region);
        config.SpeechRecognitionLanguage = settings.Language;
        using var audioConfig = AudioConfig.FromWavFileInput(filePath);
        using var recognizer = new SpeechRecognizer(config, audioConfig);
        var phrases = new ConcurrentQueue<string>();
        var finished = new TaskCompletionSource<CancellationErrorCode?>(TaskCreationOptions.RunContinuationsAsynchronously);

        recognizer.Recognized += (_, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
                phrases.Enqueue(e.Result.Text);
        };
        recognizer.Canceled += (_, e) => finished.TrySetResult(
            e.Reason == CancellationReason.Error ? e.ErrorCode : null);
        recognizer.SessionStopped += (_, _) => finished.TrySetResult(null);

        try
        {
            await recognizer.StartContinuousRecognitionAsync().WaitAsync(cancellationToken);
            var error = await finished.Task.WaitAsync(cancellationToken);
            if (error is not null)
                throw new InvalidOperationException($"Speech recognition failed ({error}).");

            return string.Join(" ", phrases);
        }
        finally
        {
            await recognizer.StopContinuousRecognitionAsync();
        }
    }
}
