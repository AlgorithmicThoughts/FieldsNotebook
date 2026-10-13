using Accessors;
using Accessors.Activity;
using Accessors.Delays;
using Accessors.Projects;
using Accessors.Users;
using Accessors.WorkItems;
using Common.Errors;
using Common.Roles;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using Engines;
using Engines.Crews;
using Engines.Delays;
using Managers.Delays;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using Utilities.OperationContract;

namespace Tests.Managers;

public class DelayInsightManagerTrackingTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;
    private const int DelayId = 7;

    [Fact]
    public async Task GetProjectTrackingDetailsAsync_NoProjectAccess_RefusesBeforeTrackingReads()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectAccessRecord?)null);

        // Act
        var result = await CreateMockManager(mocks).GetProjectTrackingDetailsAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        mocks.DelayAccessor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetProjectTrackingDetailsAsync_ProjectWithData_ReturnsTrackingAndEveryAffectedCrew()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        GrantAccess(mocks);
        SignPhotoUrls(mocks);
        using var cancellation = new CancellationTokenSource();
        mocks.DelayAccessor.Setup(a => a.GetDelayHistoryAsync(ProjectId, null, cancellation.Token)).ReturnsAsync(ReopenedDelay());

        // Act
        var result = await CreateMockManager(mocks).GetProjectTrackingDetailsAsync(UserId, ProjectId, cancellation.Token);

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
        var mocks = new DelayInsightManagerMocks();
        GrantAccess(mocks);
        mocks.DelayAccessor.Setup(a => a.GetDelayHistoryAsync(ProjectId, null, It.IsAny<CancellationToken>())).ReturnsAsync(TrackingSet.Empty);

        // Act
        var result = await CreateMockManager(mocks).GetProjectTrackingDetailsAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var tracking = ResultAssert.Succeeded(result);
        Assert.Empty(tracking.Delays);
        Assert.Equal((ProjectId, "Omaha Fabrication"), (tracking.ProjectId, tracking.ProjectName));
    }

    [Fact]
    public async Task GetDelayAsync_DelayInProject_ReturnsOnlyThatDelay()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        GrantAccess(mocks);
        SignPhotoUrls(mocks);
        mocks.DelayAccessor.Setup(a => a.GetDelayHistoryAsync(ProjectId, DelayId, It.IsAny<CancellationToken>())).ReturnsAsync(ReopenedDelay());

        // Act
        var result = await CreateMockManager(mocks).GetDelayAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        var delay = ResultAssert.Succeeded(result);
        Assert.Equal(DelayId, delay.Summary.Id);
        mocks.DelayAccessor.Verify(a => a.GetDelayHistoryAsync(It.IsAny<int>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDelayAsync_DelayNotInProject_ReturnsNotFound()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        GrantAccess(mocks);
        mocks.DelayAccessor.Setup(a => a.GetDelayHistoryAsync(ProjectId, DelayId, It.IsAny<CancellationToken>())).ReturnsAsync(TrackingSet.Empty);

        // Act
        var result = await CreateMockManager(mocks).GetDelayAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
    }

    #region Private Test Helper

    /// <summary>A DelayInsightManager over the real Engines it decides with and the mocked Accessors and Utilities it needs.</summary>
    private static DelayInsightManager CreateMockManager(DelayInsightManagerMocks mocks)
    {
        var services = new ServiceCollection();
        services.AddSingleton<AccessorFactory>();
        services.AddSingleton<EngineFactory>();
        services.AddSingleton<UtilityFactory>();
        services.AddSingleton<IDelayInsightEngine, DelayInsightEngine>();
        services.AddSingleton<ICrewCoverageEngine, CrewCoverageEngine>();
        mocks.RegistryOverride(services);

        return ActivatorUtilities.CreateInstance<DelayInsightManager>(services.BuildServiceProvider());
    }

    private static void GrantAccess(DelayInsightManagerMocks mocks)
    {
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", RoleNames.Foreman, []));
    }

    /// <summary>The photo Accessor signs every stored URL it is asked to show.</summary>
    private static void SignPhotoUrls(DelayInsightManagerMocks mocks)
    {
        mocks.DelayPhotoAccessor.Setup(a => a.GetViewUrl(It.IsAny<string>())).Returns<string>(url => $"{url}?signed");
    }

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
        public Mock<ILogger<DelayInsightManager>> Logger { get; } = new();

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
