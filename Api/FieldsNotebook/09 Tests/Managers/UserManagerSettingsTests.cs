using Accessors.Users;
using Common.Errors;
using Common.Users;
using DataContract.Me;
using Managers.Users;
using Moq;

namespace Tests.Managers
{
    public class UserManagerSettingsTests
    {
        private readonly Mock<IUserAccessor> _userAccessor = new();

        public UserManagerSettingsTests()
        {
            _userAccessor.Setup(a => a.GetUserAsync(3, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserRecord(3, "Dana Foreman", "dana@example.com", null, true));
        }

        [Fact]
        public async Task UpdateSettingsAsync_ChangedTheme_SavesOnlyThatColumnAndReturnsSavedRow()
        {
            UserSettingsChangeRecord? sent = null;
            _userAccessor
                .Setup(a => a.SaveSettingsAsync(3, It.IsAny<UserSettingsChangeRecord>(), It.IsAny<CancellationToken>()))
                .Callback<int, UserSettingsChangeRecord, CancellationToken>((_, change, _) => sent = change)
                .ReturnsAsync(new UserSettingsRecord("Dark", true, false));

            var settings = await new UserManager(_userAccessor.Object)
                .UpdateSettingsAsync(3, new UserSettingsRequest("Dark", null, null), CancellationToken.None);

            Assert.Equal(new UserSettingsChangeRecord("Dark", null, null), sent);
            Assert.Equal(new UserSettingsResponse("Dark", true, false), settings);
        }

        [Fact]
        public async Task UpdateSettingsAsync_InactiveUser_ReturnsNullWithoutSaving()
        {
            _userAccessor.Setup(a => a.GetUserAsync(4, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserRecord(4, "Former Hand", "former@example.com", null, false));

            var settings = await new UserManager(_userAccessor.Object)
                .UpdateSettingsAsync(4, new UserSettingsRequest(null, false, null), CancellationToken.None);

            Assert.Null(settings);
            VerifyNeverSaved();
        }

        [Theory]
        [InlineData("dark", null)]
        [InlineData(null, null)]
        public async Task UpdateSettingsAsync_InvalidChange_ThrowsWithoutSaving(string? theme, bool? emailEnabled)
        {
            await Assert.ThrowsAsync<InvalidRequestException>(() => new UserManager(_userAccessor.Object)
                .UpdateSettingsAsync(3, new UserSettingsRequest(theme, emailEnabled, null), CancellationToken.None));

            VerifyNeverSaved();
        }

        private void VerifyNeverSaved() =>
            _userAccessor.Verify(
                a => a.SaveSettingsAsync(It.IsAny<int>(), It.IsAny<UserSettingsChangeRecord>(), It.IsAny<CancellationToken>()),
                Times.Never);
    }
}
