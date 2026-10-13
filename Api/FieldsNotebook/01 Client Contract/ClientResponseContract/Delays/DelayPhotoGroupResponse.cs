using DataContract.Home;

namespace DataContract.Delays;

public sealed record DelayPhotoGroupResponse(
    string Purpose,
    IReadOnlyList<DelayPhotoResponse> Photos);
