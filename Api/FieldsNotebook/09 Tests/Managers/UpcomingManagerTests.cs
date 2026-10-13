using Accessors.Delays;
using Accessors.Projects;
using Accessors.WorkItems;
using Common.Delays;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Domain.Users;
using DataContract.Domain.WorkItems;
using DataContract.Upcoming;
using Managers.Upcoming;
using Moq;

namespace Tests.Managers;

public class UpcomingManagerTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;
    private const int SteelFitUp = 2;
    private const int Welding = 3;
    private const int Coatings = 4;
    private const int WeldFrame = 10;
    private const int CoatFrame = 11;
    private const int FitUpSteel = 9;

    private static readonly DateTimeOffset WeldStart = new(2026, 10, 11, 13, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CoatStart = new(2026, 10, 11, 20, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FitUpFinish = new(2026, 10, 11, 19, 0, 0, TimeSpan.Zero);

    private readonly Mock<IProjectAccessAccessor> _projectAccess = new();
    private readonly Mock<IDelayAccessor> _delays = new();
    private readonly Mock<IWorkItemAccessor> _workItems = new();

    public UpcomingManagerTests()
    {
        PlanWork(PlannedWorkSet.Empty);
    }

    [Fact]
    public async Task GetUpcomingAsync_DelayBlocksTwoCrews_BuildsARowForEachEarliestFirst()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
        var readyAt = new DateTimeOffset(2026, 10, 11, 16, 0, 0, TimeSpan.Zero);
        var advance = Delay(1, "FN-003", DelayReportTypes.AdvanceNotice, cycles: 1, expectedReadyAt: readyAt);
        ReportDelays([advance], Impact(1, Coatings, "Coatings", CoatFrame), Impact(1, Welding, "Welding and Erection", WeldFrame));
        PlanWork(new PlannedWorkSet([WorkItem(WeldFrame, "Weld bay 3 frame", WeldStart), WorkItem(CoatFrame, "Coat bay 3 frame", CoatStart)], []));

        // Act
        var result = await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var upcoming = ResultAssert.Succeeded(result);
        Assert.Equal(["Weld bay 3 frame", "Coat bay 3 frame"], upcoming.BlockedWork.Select(r => r.WorkItemTitle));
        Assert.Equal(["Welding and Erection", "Coatings"], upcoming.BlockedWork.Select(r => r.BlockedCrewName));
        var weld = upcoming.BlockedWork[0];
        Assert.Equal(("FN-003", "Steel Fit-up", "Bay 2 Steel Assembly", WeldStart), (weld.DelayNumber, weld.OwnerCrewName, weld.SegmentName, weld.PlannedStartAt));
        Assert.Equal(new CompletionEstimateResponse(CompletionEstimateSources.DelayCycle, readyAt, null), weld.Completion);
        Assert.True(weld.IsAdvanceNotice);
        Assert.False(weld.IsHighPriority);
        Assert.Null(weld.BlockedPredecessor);
    }

    [Fact]
    public async Task GetUpcomingAsync_ReopenedDelayWithoutCompletionTime_ReadsThePredecessorExpectedFinish()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
        var reopened = Delay(2, "FN-006", DelayReportTypes.AfterArrival, cycles: 2, expectedReadyAt: null);
        var onPredecessor = Delay(3, "FN-007", DelayReportTypes.AfterArrival, cycles: 1, expectedReadyAt: null);
        ReportDelays([reopened, onPredecessor], Impact(2, Welding, "Welding and Erection", WeldFrame), Impact(3, SteelFitUp, "Steel Fit-up", FitUpSteel));
        PlanWork(new PlannedWorkSet(
            [WorkItem(WeldFrame, "Weld bay 3 frame", WeldStart)],
            [new WorkItemPredecessorRecord(WeldFrame, FitUpSteel, "Fit up bay 3 steel", FitUpFinish)]));

        // Act
        var result = await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var row = Assert.Single(ResultAssert.Succeeded(result).BlockedWork);
        Assert.Equal("FN-006", row.DelayNumber);
        Assert.True(row.IsHighPriority);
        Assert.Equal(new CompletionEstimateResponse(CompletionEstimateSources.WorkItem, FitUpFinish, "Fit up bay 3 steel"), row.Completion);
        Assert.Equal(new BlockedPredecessorResponse("Fit up bay 3 steel", "FN-007"), row.BlockedPredecessor);
    }

    [Fact]
    public async Task GetUpcomingAsync_NoCompletionTimeAnywhere_ReportsNoEstimate()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
        ReportDelays([Delay(2, "FN-006", DelayReportTypes.AfterArrival, cycles: 1, expectedReadyAt: null)], Impact(2, Welding, "Welding and Erection", WeldFrame));
        PlanWork(new PlannedWorkSet(
            [WorkItem(WeldFrame, "Weld bay 3 frame", WeldStart)],
            [new WorkItemPredecessorRecord(WeldFrame, FitUpSteel, "Fit up bay 3 steel", PredecessorExpectedFinishAt: null)]));

        // Act
        var result = await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var row = Assert.Single(ResultAssert.Succeeded(result).BlockedWork);
        Assert.Equal(new CompletionEstimateResponse(CompletionEstimateSources.None, null, null), row.Completion);
        Assert.Null(row.BlockedPredecessor);
    }

    [Fact]
    public async Task GetUpcomingAsync_ImpactOutsideTheNextDayPlan_IsNotListed()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
        ReportDelays(
            [Delay(2, "FN-006", DelayReportTypes.AfterArrival, cycles: 1, expectedReadyAt: null)],
            Impact(2, Welding, "Welding and Erection", blockedWorkItemId: null),
            Impact(2, Coatings, "Coatings", CoatFrame));
        PlanWork(new PlannedWorkSet([WorkItem(WeldFrame, "Weld bay 3 frame", WeldStart)], []));

        // Act
        var result = await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        Assert.Empty(ResultAssert.Succeeded(result).BlockedWork);
    }

    [Fact]
    public async Task GetUpcomingAsync_ActiveDelays_AsksForWorkPlannedThroughTheNextProjectDay()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
        ReportDelays([Delay(2, "FN-006", DelayReportTypes.AfterArrival, cycles: 1, expectedReadyAt: null)], Impact(2, Welding, "Welding and Erection", WeldFrame));
        var before = DateTimeOffset.UtcNow;

        // Act
        await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        _workItems.Verify(
            a => a.GetPlannedWorkAsync(ProjectId, It.Is<DateTimeOffset>(d => d > before.AddDays(1) && d <= before.AddDays(3)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUpcomingAsync_NoActiveDelays_ReturnsEmptyWithoutReadingWorkItems()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
        ReportDelays([]);

        // Act
        var result = await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        Assert.Empty(ResultAssert.Succeeded(result).BlockedWork);
        VerifyNoWorkItemRead();
    }

    [Fact]
    public async Task GetUpcomingAsync_NoProjectAccess_RefusesWithoutReading()
    {
        // Act
        var act = () => CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        VerifyNoDelayRead();
        VerifyNoWorkItemRead();
    }

    [Fact]
    public async Task GetUpcomingAsync_NoCoveredCrews_ReturnsEmptyWithoutReading()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman);

        // Act
        var result = await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        Assert.Empty(ResultAssert.Succeeded(result).BlockedWork);
        VerifyNoDelayRead();
        VerifyNoWorkItemRead();
    }

    #region Private Methods

    private UpcomingManager CreateManager()
    {
        return new ManagerTestHost().With(_projectAccess).With(_delays).With(_workItems).Create<UpcomingManager>();
    }

    private static CrewLeadRecord Crew(int crewId, string name)
    {
        return new CrewLeadRecord(crewId, ProjectId, name, IsForemanLead: true);
    }

    private void GrantAccess(string roleName, params CrewLeadRecord[] crews)
    {
        _projectAccess
            .Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", roleName, crews));
    }

    private void ReportDelays(IReadOnlyList<ActiveDelayRecord> delays, params ActiveDelayImpactRecord[] impacts)
    {
        _delays
            .Setup(a => a.GetActiveDelaysForCrewsAsync(ProjectId, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActiveDelaySet(delays, impacts));
    }

    private void PlanWork(PlannedWorkSet planned)
    {
        _workItems
            .Setup(a => a.GetPlannedWorkAsync(ProjectId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(planned);
    }

    private void VerifyNoDelayRead()
    {
        _delays.Verify(
            a => a.GetActiveDelaysForCrewsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void VerifyNoWorkItemRead()
    {
        _workItems.Verify(
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

    #endregion
}
