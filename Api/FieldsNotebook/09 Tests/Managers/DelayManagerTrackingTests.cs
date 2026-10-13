using Accessors.Delays;
using Accessors.Projects;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using Managers.Delays;
using Moq;
using Utilities.OperationContract;

namespace Tests.Managers;

public class DelayManagerTrackingTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;
    private const int DelayId = 7;

    private readonly Mock<IProjectAccessAccessor> _access = new();
    private readonly Mock<IDelayAccessor> _delays = new();
    private readonly Mock<IAzureBlobStorageUtility> _storage = new();

    public DelayManagerTrackingTests()
    {
        _storage.Setup(s => s.GetReadUrl(It.IsAny<string>())).Returns<string>(url => $"{url}?signed");
    }

    [Fact]
    public async Task GetProjectTrackingDetailsAsync_NoProjectAccess_RefusesBeforeTrackingReads()
    {
        // Act
        var act = () => CreateManager().GetProjectTrackingDetailsAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        _delays.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetProjectTrackingDetailsAsync_ProjectWithData_ReturnsTrackingAndEveryAffectedCrew()
    {
        // Arrange
        GrantAccess();
        using var cancellation = new CancellationTokenSource();
        _delays.Setup(a => a.GetProjectTrackingDetailsAsync(ProjectId, cancellation.Token)).ReturnsAsync(ReopenedDelay());

        // Act
        var result = await CreateManager().GetProjectTrackingDetailsAsync(UserId, ProjectId, cancellation.Token);

        // Assert
        var tracking = ResultAssert.Succeeded(result);
        Assert.Equal((ProjectId, "Omaha Fabrication"), (tracking.ProjectId, tracking.ProjectName));
        var delay = Assert.Single(tracking.Delays);
        Assert.Equal("FN-007", delay.Summary.Number);
        Assert.True(delay.Summary.IsHighPriority);
        Assert.Equal(["Welding", "Coatings"], delay.Summary.BlockedCrewNames);
        Assert.Equal([1, 2], delay.Cycles.Select(c => c.Sequence));
        Assert.All(delay.Impacts, impact => Assert.Empty(impact.Verifications));
        var photo = Assert.Single(Assert.Single(delay.Photos).Photos);
        Assert.Equal(("original.jpg?signed", "thumbnail.jpg?signed"), (photo.OriginalUrl, photo.ThumbnailUrl));
    }

    [Fact]
    public async Task GetProjectTrackingDetailsAsync_ProjectWithoutData_ReturnsProjectNameAndEmptyTracking()
    {
        // Arrange
        GrantAccess();
        _delays.Setup(a => a.GetProjectTrackingDetailsAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(TrackingSet.Empty);

        // Act
        var result = await CreateManager().GetProjectTrackingDetailsAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var tracking = ResultAssert.Succeeded(result);
        Assert.Empty(tracking.Delays);
        Assert.Equal((ProjectId, "Omaha Fabrication"), (tracking.ProjectId, tracking.ProjectName));
    }

    [Fact]
    public async Task GetDelayAsync_DelayInProject_ReturnsOnlyThatDelay()
    {
        // Arrange
        GrantAccess();
        _delays.Setup(a => a.GetDelayTrackingDetailsAsync(ProjectId, DelayId, It.IsAny<CancellationToken>())).ReturnsAsync(ReopenedDelay());

        // Act
        var result = await CreateManager().GetDelayAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        var delay = ResultAssert.Succeeded(result);
        Assert.Equal(DelayId, delay.Summary.Id);
        _delays.Verify(a => a.GetProjectTrackingDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDelayAsync_DelayNotInProject_ReturnsNotFound()
    {
        // Arrange
        GrantAccess();
        _delays.Setup(a => a.GetDelayTrackingDetailsAsync(ProjectId, DelayId, It.IsAny<CancellationToken>())).ReturnsAsync(TrackingSet.Empty);

        // Act
        var result = await CreateManager().GetDelayAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
    }

    #region Private Methods

    private static TrackingSet ReopenedDelay()
    {
        var timestamp = DateTimeOffset.UnixEpoch;
        var summary = new ActiveDelayRecord(DelayId, "FN-007", "Steel blocker", "Missing steel", "Reopened",
            "MissingMaterial", "Missing material", 2, "Steel", "AfterArrival", "Bay 2", "Bay 2",
            null, null, null, timestamp, 2, true, timestamp.AddDays(1));
        return new TrackingSet(
            [new TrackingDelayRecord(summary, UserId, "Reporter", "Welding")],
            [new TrackingImpactRecord(10, DelayId, "Welding", 9, "Weld frame"), new TrackingImpactRecord(11, DelayId, "Coatings", null, null)],
            [new TrackingCycleRecord(1, DelayId, timestamp, null, null, null, null, null, null, timestamp.AddHours(1)),
             new TrackingCycleRecord(2, DelayId, timestamp.AddHours(2), "Still blocked", null, null, null, null, timestamp.AddDays(1), null)],
            [new TrackingVerificationRecord(10, 1, "Old verifier", timestamp, null)],
            [new TrackingPhotoRecord(1, DelayId, "InitialReport", "original.jpg", "thumbnail.jpg", "photo.jpg", timestamp, null, null)],
            [], []);
    }

    private void GrantAccess()
    {
        _access.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", RoleNames.Foreman, []));
    }

    private DelayManager CreateManager()
    {
        return new ManagerTestHost().With(_access).With(_delays).With(_storage).Create<DelayManager>();
    }

    #endregion
}
