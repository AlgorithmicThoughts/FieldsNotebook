using DataContract.Delays;
using Microsoft.Extensions.Options;
using Utilities.Speech;

namespace Managers.Delays;

public sealed class DelaySpeechManager(IDelayReportManager reports, IOptions<SpeechOptions> options) : IDelaySpeechManager
{
    public async Task<DelaySpeechResponse> TranscribeAsync(
        int userId, int projectId, DelaySpeechRequest request, CancellationToken cancellationToken)
    {
        // Reuse project access and the same live crew/tag options displayed in the report form.
        var formOptions = await reports.GetReportOptionsAsync(userId, projectId, cancellationToken);
        await using var audio = request.Audio.OpenReadStream();
        var transcript = await SpeechRecognitionUtility.RecognizeAsync(audio, options.Value, cancellationToken);
        var fields = SpeechFieldMappingUtility.MapFields(transcript,
            formOptions.Crews.Select(crew => new SpeechCrewOption(crew.Id, crew.Name)).ToArray(),
            formOptions.Tags.Select(tag => new SpeechTagOption(tag.Id, tag.Code, tag.Name)).ToArray());
        return new(fields.Title, fields.Description, fields.OwnerCrewId, fields.DelayTagId);
    }
}
