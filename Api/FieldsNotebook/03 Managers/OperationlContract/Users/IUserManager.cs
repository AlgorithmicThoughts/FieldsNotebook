using Common.Results;
using DataContract.Me;
using DataContract.Users;

namespace Managers.Users;

public interface IUserManager
{
    Task<Result<UserResponse>> GetUserAsync(int userId, CancellationToken cancellationToken);

    /// <summary>The signed-in user; not found when the user does not exist or is inactive.</summary>
    Task<Result<CurrentUserResponse>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken);

    /// <summary>Saves the changed preferences; not found when the user does not exist or is inactive.</summary>
    Task<Result<UserSettingsResponse>> UpdateSettingsAsync(int userId, UserSettingsRequest request, CancellationToken cancellationToken);
}
