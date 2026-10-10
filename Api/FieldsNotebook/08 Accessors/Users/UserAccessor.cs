using Common.Users;
using Database;
using Microsoft.EntityFrameworkCore;

namespace Accessors.Users
{
    public class UserAccessor(FieldNotebookContext context) : IUserAccessor
    {
        public Task<UserRecord?> GetUserAsync(int userId, CancellationToken cancellationToken) =>
            context.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserRecord(u.Id, u.DisplayName, u.Email, u.Phone, u.IsActive))
                .SingleOrDefaultAsync(cancellationToken);
    }
}
