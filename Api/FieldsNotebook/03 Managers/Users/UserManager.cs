using Accessors.Users;
using DataContract.Users;

namespace Managers.Users
{
    public class UserManager(IUserAccessor userAccessor) : IUserManager
    {
        public async Task<UserResponse?> GetUserAsync(int userId, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);

            var user = await userAccessor.GetUserAsync(userId, cancellationToken);
            return user is null
                ? null
                : new UserResponse(user.Id, user.DisplayName, user.Email, user.Phone, user.IsActive);
        }
    }
}
