using DataContract.Domain.Projects;
using DataContract.Domain.Users;

namespace Engines.Crews;

/// <summary>Which crews a project role speaks for.</summary>
public interface ICrewCoverageEngine : IEngine
{
    /// <summary>The crews the user's role covers in the project, by ID.</summary>
    IReadOnlySet<int> GetCoveredCrewIds(ProjectAccessRecord access);

    /// <summary>Each of the user's projects with the crews their role covers there and whether they can report a delay.</summary>
    IReadOnlyList<UserProjectCoverageRecord> BuildProjects(CurrentUserRecord user);
}
