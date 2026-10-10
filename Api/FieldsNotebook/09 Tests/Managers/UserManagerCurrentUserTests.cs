using Accessors.Users;
using Common.Roles;
using Common.Users;
using DataContract.Me;
using Managers.Users;
using Moq;

namespace Tests.Managers
{
    public class UserManagerCurrentUserTests
    {
        private readonly Mock<IUserAccessor> _userAccessor = new();

        [Fact]
        public async Task GetCurrentUserAsync_Foreman_ReturnsLedCrewAndCanReport()
        {
            SetUpUser(
                [new UserProjectRecord(1, "Omaha Fabrication", "OMA-FAB", "Northern Plains", RoleNames.Foreman)],
                [new CrewLeadRecord(3, 1, "Welding and Erection", IsForemanLead: true)]);

            var user = await new UserManager(_userAccessor.Object).GetCurrentUserAsync(3, CancellationToken.None);

            var project = Assert.Single(user!.Projects);
            Assert.Equal([new CrewResponse(3, "Welding and Erection")], project.Crews);
            Assert.True(project.CanReportDelay);
        }

        [Fact]
        public async Task GetCurrentUserAsync_Superintendent_ReturnsDirectedCrewsAndCannotReport()
        {
            SetUpUser(
                [new UserProjectRecord(1, "Omaha Fabrication", "OMA-FAB", "Northern Plains", RoleNames.Superintendent)],
                [new CrewLeadRecord(4, 1, "Coatings", IsForemanLead: false),
                 new CrewLeadRecord(3, 1, "Welding and Erection", IsForemanLead: false),
                 new CrewLeadRecord(9, 3, "Other project crew", IsForemanLead: false)]);

            var user = await new UserManager(_userAccessor.Object).GetCurrentUserAsync(6, CancellationToken.None);

            var project = Assert.Single(user!.Projects);
            Assert.Equal(["Coatings", "Welding and Erection"], project.Crews.Select(c => c.Name));
            Assert.False(project.CanReportDelay);
        }

        [Fact]
        public async Task GetCurrentUserAsync_NoSettingsRow_ReturnsDefaults()
        {
            SetUpUser([], []);

            var user = await new UserManager(_userAccessor.Object).GetCurrentUserAsync(3, CancellationToken.None);

            Assert.Equal(("System", true, false), (user!.ThemePreference, user.EmailEnabled, user.SmsEnabled));
        }

        [Fact]
        public async Task GetCurrentUserAsync_UnknownUser_ReturnsNull()
        {
            var user = await new UserManager(_userAccessor.Object).GetCurrentUserAsync(99, CancellationToken.None);

            Assert.Null(user);
        }

        [Fact]
        public async Task GetCurrentUserAsync_InvalidId_ThrowsWithoutReading()
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => new UserManager(_userAccessor.Object).GetCurrentUserAsync(0, CancellationToken.None));

            _userAccessor.Verify(a => a.GetCurrentUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private void SetUpUser(IReadOnlyList<UserProjectRecord> projects, IReadOnlyList<CrewLeadRecord> crews) =>
            _userAccessor
                .Setup(a => a.GetCurrentUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) =>
                    new CurrentUserRecord(id, "Demo User", "demo@example.com", null, Settings: null, projects, crews));
    }
}
