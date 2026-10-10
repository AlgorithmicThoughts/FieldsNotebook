using DataContract.Users;

namespace Managers.Users
{
    public interface IUserManager
    {
        Task<UserResponse?> GetUserAsync(int userId, CancellationToken cancellationToken);
    }
}
