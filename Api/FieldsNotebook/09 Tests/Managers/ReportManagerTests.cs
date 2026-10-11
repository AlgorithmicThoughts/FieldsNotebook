using Accessors.Delays;
using Accessors.Projects;
using Common.Errors;
using Common.Projects;
using Common.Reports;
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
        GrantAccess(1);
        GrantAccess(3);
        var createdAt = new DateTimeOffset(2026, 10, 5, 14, 0, 0, TimeSpan.Zero);
        var delay = new DelayReportRecord(7, "FN-007", "Omaha Fabrication", "Steel blocker", "Resolved", "Missing material",
            "AfterArrival", "Fitting", "North bay", null, "Dana Foreman", createdAt, createdAt.AddDays(1), 2, 3);
        _delays
            .Setup(a => a.GetDelayReportAsync(
                It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DelayReportSet([delay], [new(7, "Erection"), new(7, "Welding")]));

        var rows = await new ReportManager(_access.Object, _delays.Object).GetDelayReportAsync(
            UserId, new DelayReportRequest([1, 3, 1], new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)), CancellationToken.None);

        _delays.Verify(a => a.GetDelayReportAsync(
            It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 1, 3 })),
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
            It.IsAny<CancellationToken>()));
        var row = Assert.Single(rows);
        Assert.Equal("FN-007", row.DelayNumber);
        Assert.Equal(["Erection", "Welding"], row.BlockedCrewNames);
        Assert.Equal(createdAt.AddDays(1), row.ResolvedAt);
        Assert.Equal(2, row.CycleCount);
    }

    [Fact]
    public async Task GetDelayReportAsync_ProjectWithoutAccess_ThrowsWithoutReadingDelays()
    {
        GrantAccess(1);

        await Assert.ThrowsAsync<ProjectAccessDeniedException>(() => new ReportManager(_access.Object, _delays.Object)
            .GetDelayReportAsync(UserId, new DelayReportRequest([1, 2], null, null), CancellationToken.None));

        _delays.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task GetDelayReportAsync_InvalidScope_ThrowsBeforeAnyRead(DelayReportRequest request)
    {
        await Assert.ThrowsAsync<InvalidRequestException>(() => new ReportManager(_access.Object, _delays.Object)
            .GetDelayReportAsync(UserId, request, CancellationToken.None));

        _access.VerifyNoOtherCalls();
        _delays.VerifyNoOtherCalls();
    }

    public static TheoryData<DelayReportRequest> InvalidRequests() =>
    [
        new DelayReportRequest(null, null, null),
        new DelayReportRequest([], null, null),
        new DelayReportRequest([1], new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 1)),
    ];

    private void GrantAccess(int projectId) =>
        _access.Setup(a => a.GetProjectAccessAsync(UserId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(projectId, $"Project {projectId}", "Foreman", []));
}
