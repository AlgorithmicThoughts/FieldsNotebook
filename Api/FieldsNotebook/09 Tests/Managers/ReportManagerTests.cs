using Accessors.Delays;
using Accessors.Projects;
using Common.Errors;
using DataContract.Domain.Projects;
using DataContract.Domain.Reports;
using DataContract.Reports;
using Managers.Reports;
using Moq;

namespace Tests.Managers;

public class ReportManagerTests
{
    private const int UserId = 3;

    private readonly Mock<IProjectAccessAccessor> _access = new();
    private readonly Mock<IDelayAccessor> _delays = new();

    [Fact]
    public async Task GetDelayReportAsync_AccessibleScope_SendsUtcDayBoundsAndReturnsRowsWithBlockedCrews()
    {
        // Arrange
        GrantAccess(1);
        GrantAccess(3);
        var createdAt = new DateTimeOffset(2026, 10, 5, 14, 0, 0, TimeSpan.Zero);
        var delay = new DelayReportRecord(7, "FN-007", "Omaha Fabrication", "Steel blocker", "Resolved", "Missing material",
            "AfterArrival", "Fitting", "North bay", null, "Dana Foreman", createdAt, createdAt.AddDays(1), 2, 3);
        DelayReportQueryRecord? sent = null;
        _delays
            .Setup(a => a.GetDelayReportAsync(It.IsAny<DelayReportQueryRecord>(), It.IsAny<CancellationToken>()))
            .Callback<DelayReportQueryRecord, CancellationToken>((query, _) => sent = query)
            .ReturnsAsync(new DelayReportSet([delay], [new(7, "Erection"), new(7, "Welding")]));
        var request = new DelayReportRequest([1, 3, 1], new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        // Act
        var result = await CreateManager().GetDelayReportAsync(UserId, request, CancellationToken.None);

        // Assert
        var row = Assert.Single(ResultAssert.Succeeded(result));
        Assert.Equal([1, 3], sent!.ProjectIds);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), sent.CreatedFrom);
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), sent.CreatedBefore);
        Assert.Equal("FN-007", row.DelayNumber);
        Assert.Equal(["Erection", "Welding"], row.BlockedCrewNames);
        Assert.Equal((createdAt.AddDays(1), 2), (row.ResolvedAt, row.CycleCount));
    }

    [Fact]
    public async Task GetDelayReportAsync_ProjectWithoutAccess_RefusesWithoutReadingDelays()
    {
        // Arrange
        GrantAccess(1);

        // Act
        var act = () => CreateManager().GetDelayReportAsync(UserId, new DelayReportRequest([1, 2], null, null), CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        _delays.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDelayTagNamesAsync_ReturnsTheAccessorsNamesInOrder()
    {
        // Arrange
        _delays.Setup(a => a.GetActiveDelayTagNamesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(["Missing material", "Safety issue"]);

        // Act
        var names = await CreateManager().GetDelayTagNamesAsync(CancellationToken.None);

        // Assert
        Assert.Equal(["Missing material", "Safety issue"], names);
    }

    #region Private Methods

    private void GrantAccess(int projectId)
    {
        _access.Setup(a => a.GetProjectAccessAsync(UserId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(projectId, $"Project {projectId}", "Foreman", []));
    }

    private ReportManager CreateManager()
    {
        return new ManagerTestHost().With(_access).With(_delays).Create<ReportManager>();
    }

    #endregion
}
