using DataContract.Me;
using DataContract.Users;

namespace Managers.Users
{
    public interface IUserManager
    {
        Task<UserResponse?> GetUserAsync(int userId, CancellationToken cancellationToken);

        Task<CurrentUserResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken);
    }
}
