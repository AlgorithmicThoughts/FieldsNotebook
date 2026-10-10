using Accessors.Delays;
using Accessors.Projects;
using Common.Delays;
using Common.Errors;
using Common.Projects;
using Common.Roles;
using Common.Users;
using Managers.Home;
using Moq;

namespace Tests.Managers
{
    public class HomeManagerTests
    {
        private const int UserId = 3;
        private const int ProjectId = 1;
        private const int Welding = 3;
        private const int SteelFitUp = 2;
        private const int Coatings = 4;

        private readonly Mock<IProjectAccessAccessor> _projectAccess = new();
        private readonly Mock<IDelayAccessor> _delays = new();

        [Fact]
        public async Task GetHomeAsync_Foreman_BuildsMetricsListAndAdvanceNotice()
        {
            GrantAccess(RoleNames.Foreman, new CrewLeadRecord(Welding, ProjectId, "Welding and Erection", IsForemanLead: true));
            var tomorrow = new DateTimeOffset(2026, 10, 11, 16, 0, 0, TimeSpan.Zero);
            var advance = Delay(1, "FN-003", SteelFitUp, "Steel Fit-up", DelayStatusCodes.Acknowledged, DelayReportTypes.AdvanceNotice, cycles: 1, eta: tomorrow);
            var reopened = Delay(2, "FN-006", Welding, "Welding and Erection", DelayStatusCodes.Reopened, DelayReportTypes.AfterArrival, cycles: 2);
            _delays
                .Setup(a => a.GetActiveDelaysForCrewsAsync(ProjectId, It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { Welding })), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ActiveDelaySet(
                    [advance, reopened],
                    [Impact(1, Welding, "Welding and Erection"), Impact(1, Coatings, "Coatings"), Impact(1, Welding, "Welding and Erection"),
                     Impact(2, Coatings, "Coatings")]));

            var home = await CreateManager().GetHomeAsync(UserId, ProjectId, CancellationToken.None);

            Assert.Equal(1, home.Metrics.BlockingOurCrews);
            Assert.Equal(1, home.Metrics.CrewsWaitingOnUs);
            Assert.Equal(0, home.Metrics.ReadyToVerify);
            Assert.Equal(["FN-006", "FN-003"], home.AttentionDelays.Select(d => d.Number));
            Assert.True(home.AttentionDelays[0].IsHighPriority);
            Assert.Equal("FN-003", home.AdvanceNotice?.DelayNumber);
            Assert.Equal(["Welding and Erection", "Coatings"], home.AdvanceNotice?.BlockedCrewNames);
            Assert.Equal(tomorrow, home.AdvanceNotice?.ExpectedReadyAt);
        }

        [Fact]
        public async Task GetHomeAsync_ReadyForHandoverUnverified_CountsReadyToVerify()
        {
            GrantAccess(RoleNames.Foreman, new CrewLeadRecord(Welding, ProjectId, "Welding and Erection", IsForemanLead: true));
            var ready = Delay(5, "FN-005", SteelFitUp, "Steel Fit-up", DelayStatusCodes.ReadyForHandover, DelayReportTypes.AfterArrival, cycles: 1);
            _delays
                .Setup(a => a.GetActiveDelaysForCrewsAsync(ProjectId, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ActiveDelaySet([ready], [Impact(5, Welding, "Welding and Erection")]));

            var home = await CreateManager().GetHomeAsync(UserId, ProjectId, CancellationToken.None);

            Assert.Equal(1, home.Metrics.ReadyToVerify);
            Assert.Null(home.AdvanceNotice);
        }

        [Fact]
        public async Task GetHomeAsync_NoProjectAccess_ThrowsWithoutReadingDelays()
        {
            await Assert.ThrowsAsync<ProjectAccessDeniedException>(
                () => CreateManager().GetHomeAsync(UserId, ProjectId, CancellationToken.None));

            VerifyNoDelayRead();
        }

        [Fact]
        public async Task GetHomeAsync_NoCoveredCrews_ReturnsEmptyHomeWithoutReadingDelays()
        {
            GrantAccess(RoleNames.Foreman);

            var home = await CreateManager().GetHomeAsync(UserId, ProjectId, CancellationToken.None);

            Assert.Empty(home.AttentionDelays);
            Assert.Equal(0, home.Metrics.BlockingOurCrews);
            VerifyNoDelayRead();
        }

        [Theory]
        [InlineData(0, ProjectId)]
        [InlineData(UserId, 0)]
        public async Task GetHomeAsync_InvalidIds_ThrowsWithoutReading(int userId, int projectId)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => CreateManager().GetHomeAsync(userId, projectId, CancellationToken.None));

            _projectAccess.Verify(a => a.GetProjectAccessAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private HomeManager CreateManager() => new(_projectAccess.Object, _delays.Object);

        private void GrantAccess(string roleName, params CrewLeadRecord[] crews) =>
            _projectAccess
                .Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", roleName, crews));

        private void VerifyNoDelayRead() =>
            _delays.Verify(
                a => a.GetActiveDelaysForCrewsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);

        private static ActiveDelayRecord Delay(
            int id, string number, int ownerCrewId, string ownerCrewName, string statusCode, string reportType, int cycles, DateTimeOffset? eta = null) =>
            new(id, number, $"Delay {number}", "Details", statusCode, "QualityConcern", "Quality concern", ownerCrewId, ownerCrewName,
                reportType, "Bay 2", DateTimeOffset.UnixEpoch.AddDays(id), cycles, HasOpenCycle: true, eta);

        private static ActiveDelayImpactRecord Impact(int delayId, int crewId, string crewName) =>
            new(delayId, crewId, crewName, IsVerifiedInOpenCycle: false);
    }
}
