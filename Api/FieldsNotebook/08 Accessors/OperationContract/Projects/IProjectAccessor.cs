using DataContract.Domain.Delays;
using DataContract.Domain.Projects;

namespace Accessors.Projects;

/// <summary>A project's membership and the crews, segments, map and people that belong to it.</summary>
public interface IProjectAccessor : IAccessor
{
    /// <summary>Returns null unless the user, project, assignment, and role are all active.</summary>
    Task<ProjectAccessRecord?> GetProjectAccessAsync(int userId, int projectId, CancellationToken cancellationToken);

    /// <summary>The project's active crews with their Foreman lead and Superintendent, by name.</summary>
    Task<IReadOnlyList<ReportCrewRecord>> GetCrewsAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>The project's active segments, by name.</summary>
    Task<IReadOnlyList<ReportSegmentRecord>> GetSegmentsAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>The project's active site map, or null when it has none.</summary>
    Task<ReportMapRecord?> GetActiveSiteMapAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>Every active Foreman and Superintendent of the project, once for each crew they lead or direct.</summary>
    Task<IReadOnlyList<ReportPersonRecord>> GetMembersAsync(int projectId, CancellationToken cancellationToken);

    /// <summary>The active Foremen and Superintendents of the project's active crews who hold active access to the project.</summary>
    Task<IReadOnlyList<CrewContactRecord>> GetCrewContactsAsync(int projectId, CancellationToken cancellationToken);
}
