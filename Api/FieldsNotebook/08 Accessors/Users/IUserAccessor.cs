using Common.Users;

namespace Accessors.Users
{
    public interface IUserAccessor
    {
        Task<UserRecord?> GetUserAsync(int userId, CancellationToken cancellationToken);

        /// <summary>Returns null when the user does not exist or is inactive.</summary>
        Task<CurrentUserRecord?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken);
    }
}
