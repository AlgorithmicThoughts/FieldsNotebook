using DataContract.Me;
using DataContract.Users;

namespace Managers.Users
{
    public interface IUserManager
    {
        Task<UserResponse?> GetUserAsync(int userId, CancellationToken cancellationToken);

        Task<CurrentUserResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken);

        /// <summary>Saves the changed preferences. Returns null when the user does not exist or is inactive.</summary>
        Task<UserSettingsResponse?> UpdateSettingsAsync(int userId, UserSettingsRequest request, CancellationToken cancellationToken);
    }
}
