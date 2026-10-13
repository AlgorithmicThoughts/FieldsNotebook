using Accessors;
using Accessors.Delays;
using Common.Results;
using DataContract.Mapping.ClientToDomainMapping;
using DataContract.Mapping.DomainToClientMapping;
using DataContract.Reports;
using Engines;
using Engines.Authorization;

namespace Managers.Reports;

public sealed class ReportManager(AccessorFactory accessorFactory, EngineFactory engineFactory) : IReportManager
{
    private readonly IAuthorizationEngine _authorizationEngine = engineFactory.CreateEngine<IAuthorizationEngine>();
    private readonly IDelayAccessor _delayAccessor = accessorFactory.CreateAccessor<IDelayAccessor>();

    public async Task<Result<IReadOnlyList<DelayReportRowResponse>>> GetDelayReportAsync(int userId, DelayReportRequest request, CancellationToken cancellationToken)
    {
        var query = request.ToDomain();

        // Sequential on purpose: every call shares this request's DbContext.
        // A report must not widen past the projects the user can open, so one denial fails the whole scope.
        foreach (var projectId in query.ProjectIds)
        {
            await _authorizationEngine.AuthorizeProjectAsync(userId, projectId, cancellationToken);
        }

        var report = await _delayAccessor.GetDelayReportAsync(query, cancellationToken);
        return Result<IReadOnlyList<DelayReportRowResponse>>.Success(report.ToResponse());
    }

    public Task<IReadOnlyList<string>> GetDelayTagNamesAsync(CancellationToken cancellationToken)
    {
        return _delayAccessor.GetActiveDelayTagNamesAsync(cancellationToken);
    }
}
