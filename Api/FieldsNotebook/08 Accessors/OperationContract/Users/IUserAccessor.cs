using DataContract.Domain.Users;

namespace Accessors.Users;

public interface IUserAccessor : IAccessor
{
    Task<UserRecord?> GetUserAsync(int userId, CancellationToken cancellationToken);

    /// <summary>Returns null when the user does not exist or is inactive.</summary>
    Task<CurrentUserRecord?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken);

    /// <summary>Both users hold an active assignment to at least one active project.</summary>
    Task<bool> SharesActiveProjectAsync(int userId, int otherUserId, CancellationToken cancellationToken);

    /// <summary>Writes the changed columns, creating the row from the defaults when the user has none, and returns the saved row.</summary>
    Task<UserSettingsRecord> SaveSettingsAsync(int userId, UserSettingsChangeRecord change, CancellationToken cancellationToken);
}
