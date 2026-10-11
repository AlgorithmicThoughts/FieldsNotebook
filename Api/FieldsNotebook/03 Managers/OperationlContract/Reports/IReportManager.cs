using DataContract.Reports;

namespace Managers.Reports;

public interface IReportManager
{
    /// <summary>Every delay in the requested scope, newest first. The user must have access to each project.</summary>
    Task<IReadOnlyList<DelayReportRowResponse>> GetDelayReportAsync(int userId, DelayReportRequest request, CancellationToken cancellationToken);

    /// <summary>The approved reason tag names, so a breakdown can show a reason nobody raised as zero.</summary>
    Task<IReadOnlyList<string>> GetDelayTagNamesAsync(int userId, CancellationToken cancellationToken);
}
