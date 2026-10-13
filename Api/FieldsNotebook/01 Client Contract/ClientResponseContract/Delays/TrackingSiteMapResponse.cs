using DataContract.Home;

namespace DataContract.Delays;

public sealed record TrackingSiteMapResponse(
    int Id,
    string Name,
    string Uri);
