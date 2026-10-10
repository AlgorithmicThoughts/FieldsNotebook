namespace Common.Errors
{
    /// <summary>The user has no active access to the project, or the project does not exist.</summary>
    public sealed class ProjectAccessDeniedException(int projectId)
        : Exception($"The user has no active access to project {projectId}.")
    {
        public int ProjectId { get; } = projectId;
    }
}
