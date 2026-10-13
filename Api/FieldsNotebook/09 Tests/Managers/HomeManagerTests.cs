using Accessors.Activity;
using Accessors.Delays;
using Accessors.Projects;
using Common.Delays;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Activity;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Domain.Users;
using Engines.Home;
using Managers.Home;
using Moq;

namespace Tests.Managers;

public class HomeManagerTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;
    private const int Welding = 3;
    private const int SteelFitUp = 2;
    private const int Coatings = 4;

    private readonly Mock<IProjectAccessAccessor> _projectAccess = new();
    private readonly Mock<IDelayAccessor> _delays = new();
    private readonly Mock<IActivityAccessor> _activity = new();

    public HomeManagerTests()
    {
        _activity
            .Setup(a => a.GetRecentActivityForCrewsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task GetHomeAsync_Foreman_BuildsMetricsListAndAdvanceNotice()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, WeldingForeman());
        var tomorrow = new DateTimeOffset(2026, 10, 11, 16, 0, 0, TimeSpan.Zero);
        var advance = Delay(1, "FN-003", SteelFitUp, DelayStatusCodes.Acknowledged, DelayReportTypes.AdvanceNotice, cycles: 1, eta: tomorrow);
        var reopened = Delay(2, "FN-006", Welding, DelayStatusCodes.Reopened, DelayReportTypes.AfterArrival, cycles: 2);
        _delays
            .Setup(a => a.GetActiveDelaysForCrewsAsync(ProjectId, It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { Welding })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActiveDelaySet(
                [advance, reopened],
                [Impact(1, Welding, "Welding and Erection"), Impact(1, Coatings, "Coatings"), Impact(1, Welding, "Welding and Erection"),
                 Impact(2, Coatings, "Coatings")]));
        var reopenedEvent = new ActivityEventRecord(42, "delay.reopened", "FN-006", "Bay 3 weld defect returns", "Priya Shah",
            DelayStatusCodes.Resolved, DelayStatusCodes.Reopened, null, null, tomorrow.AddDays(-1));
        _activity
            .Setup(a => a.GetRecentActivityForCrewsAsync(ProjectId, It.IsAny<IReadOnlyCollection<int>>(), HomeEngine.RecentActivityLimit, It.IsAny<CancellationToken>()))
            .ReturnsAsync([reopenedEvent]);

        // Act
        var result = await CreateManager().GetHomeAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var home = ResultAssert.Succeeded(result);
        var entry = Assert.Single(home.RecentActivity);
        Assert.Equal(("delay.reopened", "FN-006", "Priya Shah", "Reopened"), (entry.EventType, entry.DelayNumber, entry.ActorName, entry.ToStatusCode));
        Assert.Equal((1, 1, 0), (home.Metrics.BlockingOurCrews, home.Metrics.CrewsWaitingOnUs, home.Metrics.ReadyToVerify));
        Assert.Equal(["FN-006", "FN-003"], home.AttentionDelays.Select(d => d.Number));
        Assert.True(home.AttentionDelays[0].IsHighPriority);
        Assert.Equal("FN-003", home.AdvanceNotice?.Number);
        Assert.Equal(["Welding and Erection", "Coatings"], home.AdvanceNotice?.BlockedCrewNames);
        Assert.Equal(tomorrow, home.AdvanceNotice?.ExpectedReadyAt);
    }

    [Fact]
    public async Task GetHomeAsync_ReadyForHandoverUnverified_CountsReadyToVerify()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, WeldingForeman());
        var ready = Delay(5, "FN-005", SteelFitUp, DelayStatusCodes.ReadyForHandover, DelayReportTypes.AfterArrival, cycles: 1);
        ReportDelays(new ActiveDelaySet([ready], [Impact(5, Welding, "Welding and Erection")]));

        // Act
        var result = await CreateManager().GetHomeAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var home = ResultAssert.Succeeded(result);
        Assert.Equal(1, home.Metrics.ReadyToVerify);
        Assert.Null(home.AdvanceNotice);
    }

    [Fact]
    public async Task GetHomeAsync_ManyActiveDelays_ListsTheAttentionLimitButCountsAll()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, WeldingForeman());
        var delays = Enumerable.Range(1, 6)
            .Select(id => Delay(id, $"FN-{id:000}", SteelFitUp, DelayStatusCodes.Raised, DelayReportTypes.AfterArrival, cycles: 1))
            .ToList();
        ReportDelays(new ActiveDelaySet(delays, delays.Select(d => Impact(d.DelayId, Welding, "Welding and Erection")).ToList()));

        // Act
        var result = await CreateManager().GetHomeAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var home = ResultAssert.Succeeded(result);
        Assert.Equal(HomeEngine.AttentionLimit, home.AttentionDelays.Count);
        Assert.Equal(6, home.Metrics.BlockingOurCrews);
    }

    [Fact]
    public async Task GetHomeAsync_NoProjectAccess_RefusesWithoutReadingDelays()
    {
        // Act
        var act = () => CreateManager().GetHomeAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        VerifyNoDelayRead();
    }

    [Fact]
    public async Task GetHomeAsync_NoIdentity_IsUnauthorizedWithoutReadingAnything()
    {
        // Act
        var act = () => CreateManager().GetHomeAsync(0, ProjectId, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Unauthorized, act);
        _projectAccess.VerifyNoOtherCalls();
        VerifyNoDelayRead();
    }

    [Fact]
    public async Task GetHomeAsync_NoCoveredCrews_ReturnsEmptyHomeWithoutReadingDelays()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman);

        // Act
        var result = await CreateManager().GetHomeAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var home = ResultAssert.Succeeded(result);
        Assert.Empty(home.AttentionDelays);
        Assert.Empty(home.RecentActivity);
        Assert.Equal(0, home.Metrics.BlockingOurCrews);
        VerifyNoDelayRead();
    }

    #region Private Methods

    private HomeManager CreateManager()
    {
        return new ManagerTestHost().With(_projectAccess).With(_delays).With(_activity).Create<HomeManager>();
    }

    private static CrewLeadRecord WeldingForeman()
    {
        return new CrewLeadRecord(Welding, ProjectId, "Welding and Erection", IsForemanLead: true);
    }

    private void GrantAccess(string roleName, params CrewLeadRecord[] crews)
    {
        _projectAccess
            .Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", roleName, crews));
    }

    private void ReportDelays(ActiveDelaySet active)
    {
        _delays
            .Setup(a => a.GetActiveDelaysForCrewsAsync(ProjectId, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);
    }

    private void VerifyNoDelayRead()
    {
        _delays.Verify(
            a => a.GetActiveDelaysForCrewsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _activity.Verify(
            a => a.GetRecentActivityForCrewsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ActiveDelayRecord Delay(int id, string number, int ownerCrewId, string statusCode, string reportType, int cycles,
        DateTimeOffset? eta = null)
    {
        return new ActiveDelayRecord(id, number, $"Delay {number}", "Details", statusCode, "QualityConcern", "Quality concern",
            ownerCrewId, $"Crew {ownerCrewId}", reportType, "Bay 2", "Bay 2 Steel Assembly", SiteMapId: null, PinX: null, PinY: null,
            DateTimeOffset.UnixEpoch.AddDays(id), cycles, HasOpenCycle: true, eta);
    }

    private static ActiveDelayImpactRecord Impact(int delayId, int crewId, string crewName)
    {
        return new ActiveDelayImpactRecord(ImpactId: delayId * 100 + crewId, delayId, crewId, crewName, BlockedWorkItemId: null,
            IsVerifiedInOpenCycle: false);
    }

    #endregion
}
