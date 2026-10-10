using Accessors.Users;
using Common.Users;
using Managers.Users;
using Moq;

namespace Tests.Managers
{
    public class UserManagerTests
    {
        private readonly Mock<IUserAccessor> _userAccessor = new();

        [Fact]
        public async Task GetUserAsync_ExistingUser_ReturnsUser()
        {
            var record = new UserRecord(7, "Dana Foreman", "dana@example.com", "+15555550100", true);
            _userAccessor.Setup(a => a.GetUserAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(record);

            var user = await new UserManager(_userAccessor.Object).GetUserAsync(7, CancellationToken.None);

            Assert.NotNull(user);
            Assert.Equal(7, user.Id);
            Assert.Equal("Dana Foreman", user.DisplayName);
            Assert.Equal("dana@example.com", user.Email);
        }

        [Fact]
        public async Task GetUserAsync_MissingUser_ReturnsNull()
        {
            var user = await new UserManager(_userAccessor.Object).GetUserAsync(99, CancellationToken.None);

            Assert.Null(user);
        }

        [Fact]
        public async Task GetUserAsync_InvalidId_ThrowsWithoutReading()
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => new UserManager(_userAccessor.Object).GetUserAsync(0, CancellationToken.None));

            _userAccessor.Verify(a => a.GetUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
