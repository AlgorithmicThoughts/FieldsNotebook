using Accessors;
using Accessors.Activity;
using Accessors.Delays;
using Accessors.Projects;
using Accessors.Users;
using Accessors.WorkItems;
using Common.Delays;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Domain.Users;
using DataContract.Domain.WorkItems;
using DataContract.Upcoming;
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

public class DelayInsightManagerUpcomingTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;
    private const int SteelFitUp = 2;
    private const int Welding = 3;
    private const int Coatings = 4;
    private const int WeldFrame = 10;
    private const int CoatFrame = 11;
    private const int FitUpSteel = 9;

    [Fact]
    public async Task GetUpcomingAsync_DelayBlocksTwoCrews_BuildsARowForEachEarliestFirst()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        mocks.ClockUtility.Setup(c => c.UtcNow).Returns(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero));
        GrantAccess(mocks, RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
        var weldStart = new DateTimeOffset(2026, 10, 11, 13, 0, 0, TimeSpan.Zero);
        var coatStart = new DateTimeOffset(2026, 10, 11, 20, 0, 0, TimeSpan.Zero);
        var readyAt = new DateTimeOffset(2026, 10, 11, 16, 0, 0, TimeSpan.Zero);
        var advance = Delay(1, "FN-003", DelayReportTypes.AdvanceNotice, cycles: 1, expectedReadyAt: readyAt);
        ReportDelays(mocks, [advance], Impact(1, Coatings, "Coatings", CoatFrame), Impact(1, Welding, "Welding and Erection", WeldFrame));
        PlanWork(mocks, new PlannedWorkSet([WorkItem(WeldFrame, "Weld bay 3 frame", weldStart), WorkItem(CoatFrame, "Coat bay 3 frame", coatStart)], []));

        // Act
        var result = await CreateMockManager(mocks).GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var upcoming = ResultAssert.Succeeded(result);
        Assert.Equal(["Weld bay 3 frame", "Coat bay 3 frame"], upcoming.BlockedWork.Select(r => r.WorkItemTitle));
        Assert.Equal(["Welding and Erection", "Coatings"], upcoming.BlockedWork.Select(r => r.BlockedCrewName));
        var weld = upcoming.BlockedWork[0];
        Assert.Equal(("FN-003", "Steel Fit-up", "Bay 2 Steel Assembly", weldStart), (weld.DelayNumber, weld.OwnerCrewName, weld.SegmentName, weld.PlannedStartAt));
        Assert.Equal(new CompletionEstimateResponse(CompletionEstimateSources.DelayCycle, readyAt, null), weld.Completion);
        Assert.True(weld.IsAdvanceNotice);
        Assert.False(weld.IsHighPriority);
        Assert.Null(weld.BlockedPredecessor);
    }

    [Fact]
    public async Task GetUpcomingAsync_ReopenedDelayWithoutCompletionTime_ReadsThePredecessorExpectedFinish()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        mocks.ClockUtility.Setup(c => c.UtcNow).Returns(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero));
        GrantAccess(mocks, RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
        var weldStart = new DateTimeOffset(2026, 10, 11, 13, 0, 0, TimeSpan.Zero);
        var fitUpFinish = new DateTimeOffset(2026, 10, 11, 19, 0, 0, TimeSpan.Zero);
        var reopened = Delay(2, "FN-006", DelayReportTypes.AfterArrival, cycles: 2, expectedReadyAt: null);
        var onPredecessor = Delay(3, "FN-007", DelayReportTypes.AfterArrival, cycles: 1, expectedReadyAt: null);
        ReportDelays(mocks, [reopened, onPredecessor], Impact(2, Welding, "Welding and Erection", WeldFrame), Impact(3, SteelFitUp, "Steel Fit-up", FitUpSteel));
        PlanWork(mocks, new PlannedWorkSet(
            [WorkItem(WeldFrame, "Weld bay 3 frame", weldStart)],
            [new WorkItemPredecessorRecord(WeldFrame, FitUpSteel, "Fit up bay 3 steel", fitUpFinish)]));

        // Act
        var result = await CreateMockManager(mocks).GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var row = Assert.Single(ResultAssert.Succeeded(result).BlockedWork);
        Assert.Equal("FN-006", row.DelayNumber);
        Assert.True(row.IsHighPriority);
        Assert.Equal(new CompletionEstimateResponse(CompletionEstimateSources.WorkItem, fitUpFinish, "Fit up bay 3 steel"), row.Completion);
        Assert.Equal(new BlockedPredecessorResponse("Fit up bay 3 steel", "FN-007"), row.BlockedPredecessor);
    }

    [Fact]
    public async Task GetUpcomingAsync_NoCompletionTimeAnywhere_ReportsNoEstimate()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        mocks.ClockUtility.Setup(c => c.UtcNow).Returns(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero));
        GrantAccess(mocks, RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
        var weldStart = new DateTimeOffset(2026, 10, 11, 13, 0, 0, TimeSpan.Zero);
        ReportDelays(mocks, [Delay(2, "FN-006", DelayReportTypes.AfterArrival, cycles: 1, expectedReadyAt: null)],
            Impact(2, Welding, "Welding and Erection", WeldFrame));
        PlanWork(mocks, new PlannedWorkSet(
            [WorkItem(WeldFrame, "Weld bay 3 frame", weldStart)],
            [new WorkItemPredecessorRecord(WeldFrame, FitUpSteel, "Fit up bay 3 steel", PredecessorExpectedFinishAt: null)]));

        // Act
        var result = await CreateMockManager(mocks).GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var row = Assert.Single(ResultAssert.Succeeded(result).BlockedWork);
        Assert.Equal(new CompletionEstimateResponse(CompletionEstimateSources.None, null, null), row.Completion);
        Assert.Null(row.BlockedPredecessor);
    }

    [Fact]
    public async Task GetUpcomingAsync_ImpactOutsideTheNextDayPlan_IsNotListed()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        mocks.ClockUtility.Setup(c => c.UtcNow).Returns(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero));
        GrantAccess(mocks, RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
        ReportDelays(mocks,
            [Delay(2, "FN-006", DelayReportTypes.AfterArrival, cycles: 1, expectedReadyAt: null)],
            Impact(2, Welding, "Welding and Erection", blockedWorkItemId: null),
            Impact(2, Coatings, "Coatings", CoatFrame));
        PlanWork(mocks, new PlannedWorkSet([WorkItem(WeldFrame, "Weld bay 3 frame", new DateTimeOffset(2026, 10, 11, 13, 0, 0, TimeSpan.Zero))], []));

        // Act
        var result = await CreateMockManager(mocks).GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        Assert.Empty(ResultAssert.Succeeded(result).BlockedWork);
    }

    [Fact]
    public async Task GetUpcomingAsync_ActiveDelays_AsksForWorkPlannedThroughTheNextProjectDay()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        // 10:00 CDT on 11 October: the next project day ends at midnight CDT on 13 October, 05:00 UTC.
        mocks.ClockUtility.Setup(c => c.UtcNow).Returns(new DateTimeOffset(2026, 10, 11, 15, 0, 0, TimeSpan.Zero));
        GrantAccess(mocks, RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
        ReportDelays(mocks, [Delay(2, "FN-006", DelayReportTypes.AfterArrival, cycles: 1, expectedReadyAt: null)],
            Impact(2, Welding, "Welding and Erection", WeldFrame));
        PlanWork(mocks, PlannedWorkSet.Empty);

        // Act
        await CreateMockManager(mocks).GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        mocks.WorkItemAccessor.Verify(
            a => a.GetPlannedWorkAsync(ProjectId, new DateTimeOffset(2026, 10, 13, 5, 0, 0, TimeSpan.Zero), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUpcomingAsync_NoActiveDelays_ReturnsEmptyWithoutReadingWorkItems()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
        ReportDelays(mocks, []);

        // Act
        var result = await CreateMockManager(mocks).GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        Assert.Empty(ResultAssert.Succeeded(result).BlockedWork);
        VerifyNoWorkItemRead(mocks);
    }

    [Fact]
    public async Task GetUpcomingAsync_NoProjectAccess_RefusesWithoutReading()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectAccessRecord?)null);

        // Act
        var act = () => CreateMockManager(mocks).GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        VerifyNoDelayRead(mocks);
        VerifyNoWorkItemRead(mocks);
    }

    [Fact]
    public async Task GetUpcomingAsync_NoCoveredCrews_ReturnsEmptyWithoutReading()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman);

        // Act
        var result = await CreateMockManager(mocks).GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        Assert.Empty(ResultAssert.Succeeded(result).BlockedWork);
        VerifyNoDelayRead(mocks);
        VerifyNoWorkItemRead(mocks);
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

    private static CrewLeadRecord Crew(int crewId, string name)
    {
        return new CrewLeadRecord(crewId, ProjectId, name, IsForemanLead: true);
    }

    private static void GrantAccess(DelayInsightManagerMocks mocks, string roleName, params CrewLeadRecord[] crews)
    {
        mocks.ProjectAccessor
            .Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", roleName, crews));
    }

    private static void ReportDelays(DelayInsightManagerMocks mocks, IReadOnlyList<ActiveDelayRecord> delays, params ActiveDelayImpactRecord[] impacts)
    {
        mocks.DelayAccessor
            .Setup(a => a.FindActiveDelaysAsync(ProjectId, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActiveDelaySet(delays, impacts));
    }

    private static void PlanWork(DelayInsightManagerMocks mocks, PlannedWorkSet planned)
    {
        mocks.WorkItemAccessor
            .Setup(a => a.GetPlannedWorkAsync(ProjectId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planned);
    }

    private static void VerifyNoDelayRead(DelayInsightManagerMocks mocks)
    {
        mocks.DelayAccessor.Verify(
            a => a.FindActiveDelaysAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static void VerifyNoWorkItemRead(DelayInsightManagerMocks mocks)
    {
        mocks.WorkItemAccessor.Verify(
            a => a.GetPlannedWorkAsync(It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ActiveDelayRecord Delay(int id, string number, string reportType, int cycles, DateTimeOffset? expectedReadyAt)
    {
        return new ActiveDelayRecord(id, number, $"Delay {number}", "Details", DelayStatusCodes.Raised, "QualityConcern", "Quality concern",
            SteelFitUp, "Steel Fit-up", reportType, "Bay 2", "Bay 2 Steel Assembly", SiteMapId: null, PinX: null, PinY: null,
            DateTimeOffset.UnixEpoch.AddDays(id), cycles, HasOpenCycle: true, expectedReadyAt);
    }

    private static ActiveDelayImpactRecord Impact(int delayId, int crewId, string crewName, int? blockedWorkItemId)
    {
        return new ActiveDelayImpactRecord(ImpactId: delayId * 100 + crewId, delayId, crewId, crewName, blockedWorkItemId,
            IsVerifiedInOpenCycle: false);
    }

    private static PlannedWorkItemRecord WorkItem(int id, string title, DateTimeOffset plannedStartAt)
    {
        return new PlannedWorkItemRecord(id, title, "Bay 2 Steel Assembly", plannedStartAt, ExpectedFinishAt: null);
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
