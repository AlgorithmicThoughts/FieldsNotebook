using DataContract.Delays;

namespace Managers.Delays;

public interface IDelaySpeechManager
{
    Task<DelaySpeechResponse> TranscribeAsync(
        int userId, int projectId, DelaySpeechRequest request, CancellationToken cancellationToken);
}
