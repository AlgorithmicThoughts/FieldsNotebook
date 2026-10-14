using Accessors.Users;
using DataContract.Domain.Users;
using Database;
using Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Tests.Accessors;

/// <summary>
/// The user Accessor against an EF Core in-memory store. Each test seeds its own database through one context and
/// reads through another, so every result comes from the store rather than the change tracker. The in-memory provider
/// proves the query's logic, not its SQL translation.
/// </summary>
public class UserAccessorTests
{
    # region GetUserAsync

    [Fact]
    public async Task GetUserAsync_ExistingUser_ReturnsEveryField()
    {
        // Arrange
        var database = new UserAccessorDatabase();
        await database.SeedAsync(new User { Id = 7, DisplayName = "Dana Foreman", Email = "dana@example.com", Phone = "+15555550100", IsActive = true });
        await using var context = database.CreateContext();

        // Act
        var user = await CreateAccessor(context).GetUserAsync(7, CancellationToken.None);

        // Assert
        Assert.Equal(new UserRecord(7, "Dana Foreman", "dana@example.com", "+15555550100", IsActive: true), user);
    }

    [Fact]
    public async Task GetUserAsync_InactiveUser_ReturnsNull()
    {
        // Arrange
        var database = new UserAccessorDatabase();
        await database.SeedAsync(new User { Id = 8, DisplayName = "Lee Retired", Email = "lee@example.com", IsActive = false });
        await using var context = database.CreateContext();

        // Act
        var user = await CreateAccessor(context).GetUserAsync(8, CancellationToken.None);

        // Assert
        // The Accessor reads the row as stored; deciding what an inactive user may do belongs to the Managers.
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserAsync_MissingUser_ReturnsNull()
    {
        // Arrange
        var database = new UserAccessorDatabase();
        await database.SeedAsync(new User { Id = 1, DisplayName = "Avery Foreman", Email = "avery@example.com", IsActive = true });
        await using var context = database.CreateContext();

        // Act
        var user = await CreateAccessor(context).GetUserAsync(99, CancellationToken.None);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserAsync_CancelledRequest_StopsTheRead()
    {
        // Arrange
        var database = new UserAccessorDatabase();
        await database.SeedAsync(new User { Id = 7, DisplayName = "Dana Foreman", Email = "dana@example.com", IsActive = true });
        await using var context = database.CreateContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var read = () => CreateAccessor(context).GetUserAsync(7, cancellation.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(read);
    }

    #endregion

    #region Private Test Helper

    private static UserAccessor CreateAccessor(FieldNotebookContext context)
    {
        return new UserAccessor(context);
    }

    /// <summary>A uniquely named in-memory database, so no test sees another test's rows.</summary>
    private sealed class UserAccessorDatabase
    {
        private readonly DbContextOptions<FieldNotebookContext> _options = new DbContextOptionsBuilder<FieldNotebookContext>()
            .UseInMemoryDatabase($"{nameof(UserAccessorTests)}-{Guid.NewGuid():N}")
            .Options;

        /// <summary>Writes the rows through a context of its own, so the test's context starts with nothing tracked.</summary>
        public async Task SeedAsync(params User[] users)
        {
            await using var context = CreateContext();
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }

        public FieldNotebookContext CreateContext()
        {
            return new FieldNotebookContext(_options);
        }
    }

    #endregion
}
