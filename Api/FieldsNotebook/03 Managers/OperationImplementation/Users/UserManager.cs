using Accessors;
using Accessors.Users;
using Common.Errors;
using Common.Results;
using DataContract.Mapping.ClientToDomainMapping;
using DataContract.Mapping.DomainToClientMapping;
using DataContract.Me;
using DataContract.Users;
using Engines.Crews;

namespace Managers.Users;

public sealed class UserManager(AccessorFactory accessorFactory) : IUserManager
{
    private readonly IUserAccessor _userAccessor = accessorFactory.CreateAccessor<IUserAccessor>();

    public async Task<Result<UserResponse>> GetUserAsync(int userId, CancellationToken cancellationToken)
    {
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

        return user.ToResponse(CrewCoverageEngine.BuildProjects(user));
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
}
