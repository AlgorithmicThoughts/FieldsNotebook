using Accessors.Delays;
using Accessors.Projects;
using Accessors.WorkItems;
using Common.Delays;
using Common.Errors;
using Common.Projects;
using Common.Roles;
using Common.Users;
using Common.WorkItems;
using DataContract.Upcoming;
using Managers.Upcoming;
using Moq;

namespace Tests.Managers
{
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

        public UpcomingManagerTests() => PlanWork(PlannedWorkSet.Empty);

        [Fact]
        public async Task GetUpcomingAsync_DelayBlocksTwoCrews_BuildsARowForEachEarliestFirst()
        {
            GrantAccess(RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
            var readyAt = new DateTimeOffset(2026, 10, 11, 16, 0, 0, TimeSpan.Zero);
            var advance = Delay(1, "FN-003", DelayReportTypes.AdvanceNotice, cycles: 1, expectedReadyAt: readyAt);
            // The later work item first, so the response order comes from the planned start and not the input.
            ReportDelays([advance], Impact(1, Coatings, "Coatings", CoatFrame), Impact(1, Welding, "Welding and Erection", WeldFrame));
            PlanWork(new PlannedWorkSet([WorkItem(WeldFrame, "Weld bay 3 frame", WeldStart), WorkItem(CoatFrame, "Coat bay 3 frame", CoatStart)], []));

            var upcoming = await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

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
            GrantAccess(RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
            var reopened = Delay(2, "FN-006", DelayReportTypes.AfterArrival, cycles: 2, expectedReadyAt: null);
            var onPredecessor = Delay(3, "FN-007", DelayReportTypes.AfterArrival, cycles: 1, expectedReadyAt: null);
            ReportDelays(
                [reopened, onPredecessor],
                Impact(2, Welding, "Welding and Erection", WeldFrame),
                Impact(3, SteelFitUp, "Steel Fit-up", FitUpSteel));
            PlanWork(new PlannedWorkSet(
                [WorkItem(WeldFrame, "Weld bay 3 frame", WeldStart)],
                [new WorkItemPredecessorRecord(WeldFrame, FitUpSteel, "Fit up bay 3 steel", FitUpFinish)]));

            var upcoming = await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

            var row = Assert.Single(upcoming.BlockedWork);
            Assert.Equal("FN-006", row.DelayNumber);
            Assert.True(row.IsHighPriority);
            Assert.Equal(new CompletionEstimateResponse(CompletionEstimateSources.WorkItem, FitUpFinish, "Fit up bay 3 steel"), row.Completion);
            Assert.Equal(new BlockedPredecessorResponse("Fit up bay 3 steel", "FN-007"), row.BlockedPredecessor);
        }

        [Fact]
        public async Task GetUpcomingAsync_NoCompletionTimeAnywhere_ReportsNoEstimate()
        {
            GrantAccess(RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
            ReportDelays([Delay(2, "FN-006", DelayReportTypes.AfterArrival, cycles: 1, expectedReadyAt: null)], Impact(2, Welding, "Welding and Erection", WeldFrame));
            PlanWork(new PlannedWorkSet(
                [WorkItem(WeldFrame, "Weld bay 3 frame", WeldStart)],
                [new WorkItemPredecessorRecord(WeldFrame, FitUpSteel, "Fit up bay 3 steel", PredecessorExpectedFinishAt: null)]));

            var upcoming = await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

            var row = Assert.Single(upcoming.BlockedWork);
            Assert.Equal(new CompletionEstimateResponse(CompletionEstimateSources.None, null, null), row.Completion);
            Assert.Null(row.BlockedPredecessor);
        }

        [Fact]
        public async Task GetUpcomingAsync_ImpactOutsideTheNextDayPlan_IsNotListed()
        {
            GrantAccess(RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
            ReportDelays(
                [Delay(2, "FN-006", DelayReportTypes.AfterArrival, cycles: 1, expectedReadyAt: null)],
                Impact(2, Welding, "Welding and Erection", blockedWorkItemId: null),
                Impact(2, Coatings, "Coatings", CoatFrame));
            PlanWork(new PlannedWorkSet([WorkItem(WeldFrame, "Weld bay 3 frame", WeldStart)], []));

            var upcoming = await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

            Assert.Empty(upcoming.BlockedWork);
        }

        [Fact]
        public async Task GetUpcomingAsync_ActiveDelays_AsksForWorkPlannedThroughTheNextProjectDay()
        {
            GrantAccess(RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
            ReportDelays([Delay(2, "FN-006", DelayReportTypes.AfterArrival, cycles: 1, expectedReadyAt: null)], Impact(2, Welding, "Welding and Erection", WeldFrame));
            var before = DateTimeOffset.UtcNow;

            await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

            // The bound must reach past tomorrow without pulling in work beyond the next project day.
            _workItems.Verify(
                a => a.GetPlannedWorkAsync(ProjectId, It.Is<DateTimeOffset>(d => d > before.AddDays(1) && d <= before.AddDays(3)), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetUpcomingAsync_NoActiveDelays_ReturnsEmptyWithoutReadingWorkItems()
        {
            GrantAccess(RoleNames.Foreman, Crew(Welding, "Welding and Erection"));
            ReportDelays([]);

            var upcoming = await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

            Assert.Empty(upcoming.BlockedWork);
            VerifyNoWorkItemRead();
        }

        [Fact]
        public async Task GetUpcomingAsync_NoProjectAccess_ThrowsWithoutReading()
        {
            await Assert.ThrowsAsync<ProjectAccessDeniedException>(
                () => CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None));

            VerifyNoDelayRead();
            VerifyNoWorkItemRead();
        }

        [Fact]
        public async Task GetUpcomingAsync_NoCoveredCrews_ReturnsEmptyWithoutReading()
        {
            GrantAccess(RoleNames.Foreman);

            var upcoming = await CreateManager().GetUpcomingAsync(UserId, ProjectId, CancellationToken.None);

            Assert.Empty(upcoming.BlockedWork);
            VerifyNoDelayRead();
            VerifyNoWorkItemRead();
        }

        [Theory]
        [InlineData(0, ProjectId)]
        [InlineData(UserId, 0)]
        public async Task GetUpcomingAsync_InvalidIds_ThrowsWithoutReading(int userId, int projectId)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => CreateManager().GetUpcomingAsync(userId, projectId, CancellationToken.None));

            _projectAccess.Verify(a => a.GetProjectAccessAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private UpcomingManager CreateManager() => new(_projectAccess.Object, _delays.Object, _workItems.Object);

        private static CrewLeadRecord Crew(int crewId, string name) => new(crewId, ProjectId, name, IsForemanLead: true);

        private void GrantAccess(string roleName, params CrewLeadRecord[] crews) =>
            _projectAccess
                .Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", roleName, crews));

        private void ReportDelays(IReadOnlyList<ActiveDelayRecord> delays, params ActiveDelayImpactRecord[] impacts) =>
            _delays
                .Setup(a => a.GetActiveDelaysForCrewsAsync(ProjectId, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ActiveDelaySet(delays, impacts));

        private void PlanWork(PlannedWorkSet planned) =>
            _workItems
                .Setup(a => a.GetPlannedWorkAsync(ProjectId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(planned);

        private void VerifyNoDelayRead() =>
            _delays.Verify(
                a => a.GetActiveDelaysForCrewsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);

        private void VerifyNoWorkItemRead() =>
            _workItems.Verify(
                a => a.GetPlannedWorkAsync(It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
                Times.Never);

        private static ActiveDelayRecord Delay(int id, string number, string reportType, int cycles, DateTimeOffset? expectedReadyAt) =>
            new(id, number, $"Delay {number}", "Details", DelayStatusCodes.Raised, "QualityConcern", "Quality concern", SteelFitUp, "Steel Fit-up",
                reportType, "Bay 2", "Bay 2 Steel Assembly", SiteMapId: null, PinX: null, PinY: null,
                DateTimeOffset.UnixEpoch.AddDays(id), cycles, HasOpenCycle: true, expectedReadyAt);

        private static ActiveDelayImpactRecord Impact(int delayId, int crewId, string crewName, int? blockedWorkItemId) =>
            new(ImpactId: delayId * 100 + crewId, delayId, crewId, crewName, blockedWorkItemId, IsVerifiedInOpenCycle: false);

        private static PlannedWorkItemRecord WorkItem(int id, string title, DateTimeOffset plannedStartAt) =>
            new(id, title, "Bay 2 Steel Assembly", plannedStartAt, ExpectedFinishAt: null);
    }
}
