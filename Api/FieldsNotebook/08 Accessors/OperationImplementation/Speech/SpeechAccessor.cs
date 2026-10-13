using System.Collections.Concurrent;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Options;

namespace Accessors.Speech
{
    public class SpeechAccessor(IOptions<SpeechOptions> options) : ISpeechAccessor
    {
        public async Task<string> TranscribeAsync(Stream audio, CancellationToken cancellationToken)
        {
            // The SDK reads WAV input from a file, not a stream.
            var filePath = Path.GetTempFileName();
            try
            {
                await using (var file = File.Create(filePath))
                    await audio.CopyToAsync(file, cancellationToken);

                return await RecognizeFileAsync(filePath, options.Value, cancellationToken);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        #region Private Methods

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

        #endregion
    }
}
