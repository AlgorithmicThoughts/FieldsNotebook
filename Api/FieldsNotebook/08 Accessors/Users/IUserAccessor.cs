using Common.Users;

namespace Accessors.Users
{
    public interface IUserAccessor
    {
        Task<UserRecord?> GetUserAsync(int userId, CancellationToken cancellationToken);
    }
}
