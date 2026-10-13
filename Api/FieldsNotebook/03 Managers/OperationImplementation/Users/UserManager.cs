using Accessors;
using Accessors.Users;
using Common.Errors;
using Common.Results;
using DataContract.Mapping.ClientToDomainMapping;
using DataContract.Mapping.DomainToClientMapping;
using DataContract.Me;
using DataContract.Users;
using Engines;
using Engines.Crews;

namespace Managers.Users;

public sealed class UserManager(AccessorFactory accessorFactory, EngineFactory engineFactory) : IUserManager
{
    private readonly IUserAccessor _userAccessor = accessorFactory.CreateAccessor<IUserAccessor>();
    private readonly ICrewCoverageEngine _crewCoverageEngine = engineFactory.CreateEngine<ICrewCoverageEngine>();

    public async Task<Result<UserResponse>> GetUserAsync(int requesterId, int userId, CancellationToken cancellationToken)
    {
        // Someone the requester may not see reads as missing, so the endpoint cannot be used to discover users.
        if (requesterId != userId && await MaySeeAsync(requesterId, userId, cancellationToken) is false)
        {
            return UserErrors.NotFound(userId);
        }

        var user = await _userAccessor.GetUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(userId);
        }

        return user.ToResponse();
    }

    public async Task<Result<CurrentUserResponse>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _userAccessor.GetCurrentUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(userId);
        }

        return user.ToResponse(_crewCoverageEngine.BuildProjects(user).Select(p => p.ToResponse()));
    }

    public async Task<Result<UserSettingsResponse>> UpdateSettingsAsync(int userId, UserSettingsRequest request, CancellationToken cancellationToken)
    {
        var user = await _userAccessor.GetUserAsync(userId, cancellationToken);
        if (user is { IsActive: true })
        {
            var saved = await _userAccessor.SaveSettingsAsync(userId, request.ToDomain(), cancellationToken);
            return saved.ToResponse();
        }

        return UserErrors.NotFound(userId);
    }

    #region Private Methods

    /// <summary>An active requester sees the people they share an active project with.</summary>
    private async Task<bool> MaySeeAsync(int requesterId, int userId, CancellationToken cancellationToken)
    {
        var requester = await _userAccessor.GetUserAsync(requesterId, cancellationToken);
        return requester is { IsActive: true } && await _userAccessor.SharesActiveProjectAsync(requesterId, userId, cancellationToken);
    }

    #endregion
}
