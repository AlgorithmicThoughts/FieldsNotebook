using Common.Users;
using DataContract.Domain.Users;
using Database;
using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Accessors.Users;

public class UserAccessor(FieldNotebookContext context) : IUserAccessor
{
    public Task<UserRecord?> GetUserAsync(int userId, CancellationToken cancellationToken) =>
        context.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new UserRecord(u.Id, u.DisplayName, u.Email, u.Phone, u.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<CurrentUserRecord?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await (
            from u in context.Users.AsNoTracking()
            join s in context.UserSettings.AsNoTracking() on u.Id equals s.UserId into settings
            from s in settings.DefaultIfEmpty()
            where u.Id == userId && u.IsActive
            select new
            {
                u.DisplayName,
                u.Email,
                u.Phone,
                Settings = s == null ? null : new UserSettingsRecord(s.ThemePreference, s.EmailEnabled, s.SmsEnabled),
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            return null;
        }

        var projects = await (
            from up in context.UserProjects.AsNoTracking()
            join p in context.Projects.AsNoTracking() on up.ProjectId equals p.Id
            join d in context.Districts.AsNoTracking() on p.DistrictId equals d.Id
            join ur in context.UserRoles.AsNoTracking() on new { up.UserId, up.ProjectId } equals new { ur.UserId, ur.ProjectId }
            join r in context.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where up.UserId == userId && up.IsActive && p.IsActive && ur.IsActive && r.IsActive
            orderby p.Name
            select new UserProjectRecord(p.Id, p.Name, p.Code, d.Name, r.Name))
            .ToListAsync(cancellationToken);

        var crews = await context.Crew.AsNoTracking()
            .Where(c => c.IsActive && (c.ForemanLeadId == userId || c.SuperintendentUserId == userId))
            .OrderBy(c => c.Name)
            .Select(c => new CrewLeadRecord(c.Id, c.ProjectId, c.Name, c.ForemanLeadId == userId))
            .ToListAsync(cancellationToken);

        return new CurrentUserRecord(userId, user.DisplayName, user.Email, user.Phone, user.Settings, projects, crews);
    }

    public Task<bool> SharesActiveProjectAsync(int userId, int otherUserId, CancellationToken cancellationToken) =>
        (from mine in context.UserProjects.AsNoTracking()
         join theirs in context.UserProjects.AsNoTracking() on mine.ProjectId equals theirs.ProjectId
         join p in context.Projects.AsNoTracking() on mine.ProjectId equals p.Id
         where mine.UserId == userId && theirs.UserId == otherUserId && mine.IsActive && theirs.IsActive && p.IsActive
         select mine.ProjectId)
            .AnyAsync(cancellationToken);

    public async Task<UserSettingsRecord> SaveSettingsAsync(int userId, UserSettingsChangeRecord change, CancellationToken cancellationToken)
    {
        // One UPDATE that sets only the changed columns, so a theme save and a channel save
        // arriving together cannot overwrite each other's value.
        var updated = await context.UserSettings
            .Where(s => s.UserId == userId)
            .ExecuteUpdateAsync(setters => SetChangedColumns(setters, change), cancellationToken);
        if (updated == 0)
        {
            await InsertSettingsAsync(userId, change, cancellationToken);
        }

        return await context.UserSettings.AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new UserSettingsRecord(s.ThemePreference, s.EmailEnabled, s.SmsEnabled))
            .SingleAsync(cancellationToken);
    }

    #region Private Methods

    private static void SetChangedColumns(UpdateSettersBuilder<UserSettings> setters, UserSettingsChangeRecord change)
    {
        if (change.ThemePreference is { } theme)
        {
            setters.SetProperty(s => s.ThemePreference, theme);
        }

        if (change.EmailEnabled is { } email)
        {
            setters.SetProperty(s => s.EmailEnabled, email);
        }

        if (change.SmsEnabled is { } sms)
        {
            setters.SetProperty(s => s.SmsEnabled, sms);
        }
    }

    private async Task InsertSettingsAsync(int userId, UserSettingsChangeRecord change, CancellationToken cancellationToken)
    {
        var defaults = UserSettingsDefaults.Value;
        context.UserSettings.Add(new UserSettings
        {
            UserId = userId,
            ThemePreference = change.ThemePreference ?? defaults.ThemePreference,
            EmailEnabled = change.EmailEnabled ?? defaults.EmailEnabled,
            SmsEnabled = change.SmsEnabled ?? defaults.SmsEnabled,
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    #endregion
}
