using Accessors;
using Accessors.Activity;
using Accessors.Delays;
using Accessors.Projects;
using Accessors.Users;
using Accessors.WorkItems;
using Common.Delays;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Activity;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Domain.Users;
using Engines;
using Engines.Crews;
using Engines.Delays;
using Engines.Authorization;
using Managers.Delays;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using Utilities.OperationContract;

namespace Tests.Managers;

public class DelayInsightManagerHomeTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;
    private const int Welding = 3;
    private const int SteelFitUp = 2;
    private const int Coatings = 4;

    [Fact]
    public async Task GetHomeAsync_Foreman_BuildsMetricsListAndAdvanceNotice()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman, WeldingForeman());
        var tomorrow = new DateTimeOffset(2026, 10, 11, 16, 0, 0, TimeSpan.Zero);
        var advance = Delay(1, "FN-003", SteelFitUp, DelayStatusCodes.Acknowledged, DelayReportTypes.AdvanceNotice, cycles: 1, eta: tomorrow);
        var reopened = Delay(2, "FN-006", Welding, DelayStatusCodes.Reopened, DelayReportTypes.AfterArrival, cycles: 2);
        mocks.DelayAccessor
            .Setup(a => a.FindActiveDelaysAsync(ProjectId, It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { Welding })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActiveDelaySet(
                [advance, reopened],
                [Impact(1, Welding, "Welding and Erection"), Impact(1, Coatings, "Coatings"), Impact(1, Welding, "Welding and Erection"),
                 Impact(2, Coatings, "Coatings")]));
        var reopenedEvent = new ActivityEventRecord(42, "delay.reopened", "FN-006", "Bay 3 weld defect returns", "Priya Shah",
            DelayStatusCodes.Resolved, DelayStatusCodes.Reopened, null, null, tomorrow.AddDays(-1));
        mocks.ActivityAccessor
            .Setup(a => a.GetRecentActivityForCrewsAsync(ProjectId, It.IsAny<IReadOnlyCollection<int>>(), DelayInsightEngine.RecentActivityLimitValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync([reopenedEvent]);

        // Act
        var result = await CreateMockManager(mocks).GetHomeAsync(UserId, ProjectId, CancellationToken.None);

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
        var mocks = new DelayInsightManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman, WeldingForeman());
        var ready = Delay(5, "FN-005", SteelFitUp, DelayStatusCodes.ReadyForHandover, DelayReportTypes.AfterArrival, cycles: 1);
        ReportDelays(mocks, new ActiveDelaySet([ready], [Impact(5, Welding, "Welding and Erection")]));

        // Act
        var result = await CreateMockManager(mocks).GetHomeAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var home = ResultAssert.Succeeded(result);
        Assert.Equal(1, home.Metrics.ReadyToVerify);
        Assert.Null(home.AdvanceNotice);
    }

    [Fact]
    public async Task GetHomeAsync_ManyActiveDelays_ListsTheAttentionLimitButCountsAll()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman, WeldingForeman());
        var delays = Enumerable.Range(1, 6)
            .Select(id => Delay(id, $"FN-{id:000}", SteelFitUp, DelayStatusCodes.Raised, DelayReportTypes.AfterArrival, cycles: 1))
            .ToList();
        ReportDelays(mocks, new ActiveDelaySet(delays, delays.Select(d => Impact(d.DelayId, Welding, "Welding and Erection")).ToList()));

        // Act
        var result = await CreateMockManager(mocks).GetHomeAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var home = ResultAssert.Succeeded(result);
        Assert.Equal(DelayInsightEngine.AttentionLimit, home.AttentionDelays.Count);
        Assert.Equal(6, home.Metrics.BlockingOurCrews);
    }

    [Fact]
    public async Task GetHomeAsync_NoProjectAccess_RefusesWithoutReadingDelays()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectAccessRecord?)null);

        // Act
        var act = () => CreateMockManager(mocks).GetHomeAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        VerifyNoDelayRead(mocks);
    }

    [Fact]
    public async Task GetHomeAsync_NoCoveredCrews_ReturnsEmptyHomeWithoutReadingDelays()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman);

        // Act
        var result = await CreateMockManager(mocks).GetHomeAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var home = ResultAssert.Succeeded(result);
        Assert.Empty(home.AttentionDelays);
        Assert.Empty(home.RecentActivity);
        Assert.Equal(0, home.Metrics.BlockingOurCrews);
        VerifyNoDelayRead(mocks);
    }

    #region Private Test Helper

    /// <summary>A DelayInsightManager over the real Engines it decides with and the mocked Accessors and Utilities it needs.</summary>
    private static DelayInsightManager CreateMockManager(DelayInsightManagerMocks mocks)
    {
        var services = new ServiceCollection();
        services.AddSingleton<AccessorFactory>();
        services.AddSingleton<EngineFactory>();
        services.AddSingleton<IAuthorizationEngine, AuthorizationEngine>();
        services.AddSingleton<UtilityFactory>();
        services.AddSingleton<IDelayInsightEngine, DelayInsightEngine>();
        services.AddSingleton<ICrewCoverageEngine, CrewCoverageEngine>();
        mocks.RegistryOverride(services);

        return ActivatorUtilities.CreateInstance<DelayInsightManager>(services.BuildServiceProvider());
    }

    private static CrewLeadRecord WeldingForeman()
    {
        return new CrewLeadRecord(Welding, ProjectId, "Welding and Erection", IsForemanLead: true);
    }

    private static void GrantAccess(DelayInsightManagerMocks mocks, string roleName, params CrewLeadRecord[] crews)
    {
        mocks.ProjectAccessor
            .Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", roleName, crews));
    }

    private static void ReportDelays(DelayInsightManagerMocks mocks, ActiveDelaySet active)
    {
        mocks.DelayAccessor
            .Setup(a => a.FindActiveDelaysAsync(ProjectId, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(active);
    }

    private static void VerifyNoDelayRead(DelayInsightManagerMocks mocks)
    {
        mocks.DelayAccessor.Verify(
            a => a.FindActiveDelaysAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mocks.ActivityAccessor.Verify(
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

    /// <summary>The Accessors and Utilities DelayInsightManager resolves, each a mock the test arranges for itself.</summary>
    private sealed class DelayInsightManagerMocks
    {
        public Mock<IProjectAccessor> ProjectAccessor { get; } = new();
        public Mock<IDelayAccessor> DelayAccessor { get; } = new();
        public Mock<IActivityAccessor> ActivityAccessor { get; } = new();
        public Mock<IWorkItemAccessor> WorkItemAccessor { get; } = new();
        public Mock<IDelayPhotoAccessor> DelayPhotoAccessor { get; } = new();
        public Mock<IUserAccessor> UserAccessor { get; } = new();
        public Mock<IClockUtility> ClockUtility { get; } = new();
        public Mock<ILogger<AuthorizationEngine>> Logger { get; } = new();

        /// <summary>Registers each mock in place of the concrete class the factories would otherwise resolve.</summary>
        public void RegistryOverride(IServiceCollection services)
        {
            services.AddSingleton(ProjectAccessor.Object);
            services.AddSingleton(DelayAccessor.Object);
            services.AddSingleton(ActivityAccessor.Object);
            services.AddSingleton(WorkItemAccessor.Object);
            services.AddSingleton(DelayPhotoAccessor.Object);
            services.AddSingleton(UserAccessor.Object);
            services.AddSingleton(ClockUtility.Object);
            services.AddSingleton(Logger.Object);
        }
    }

    #endregion
}
