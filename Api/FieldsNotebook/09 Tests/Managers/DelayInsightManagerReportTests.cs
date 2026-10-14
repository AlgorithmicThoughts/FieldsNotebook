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
using DataContract.Domain.Reports;
using DataContract.Domain.Users;
using DataContract.Reports;
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

public class DelayInsightManagerReportTests
{
    private const int UserId = 3;

    [Fact]
    public async Task GetDelayReportAsync_AccessibleScope_SendsUtcDayBoundsAndReturnsRowsWithBlockedCrews()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        GrantAccess(mocks, 1);
        GrantAccess(mocks, 3);
        var createdAt = new DateTimeOffset(2026, 10, 5, 14, 0, 0, TimeSpan.Zero);
        var delay = new DelayReportRecord(7, "FN-007", "Omaha Fabrication", "Steel blocker", "Resolved", "Missing material",
            "AfterArrival", "Fitting", "North bay", null, "Dana Foreman", createdAt, createdAt.AddDays(1), 2, 3);
        DelayReportQueryRecord? sent = null;
        mocks.DelayAccessor
            .Setup(a => a.FindDelaysAsync(It.IsAny<DelayReportQueryRecord>(), It.IsAny<CancellationToken>()))
            .Callback<DelayReportQueryRecord, CancellationToken>((query, _) => sent = query)
            .ReturnsAsync(new DelayReportSet([delay], [new(7, "Erection"), new(7, "Welding")]));
        var request = new DelayReportRequest([1, 3, 1], new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        // Act
        var result = await CreateMockManager(mocks).GetDelayReportAsync(UserId, request, CancellationToken.None);

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
        var mocks = new DelayInsightManagerMocks();
        GrantAccess(mocks, 1);

        // Act
        var act = () => CreateMockManager(mocks).GetDelayReportAsync(UserId, new DelayReportRequest([1, 2], null, null), CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        mocks.DelayAccessor.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDelayTagNamesAsync_ActiveUser_ReturnsTheAccessorsNamesInOrder()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        mocks.UserAccessor.Setup(a => a.GetUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRecord(UserId, "Dana Foreman", "dana@example.com", null, IsActive: true));
        mocks.DelayAccessor.Setup(a => a.GetDelayTagsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ReportTagRecord(1, "MissingMaterial", "Missing material"), new ReportTagRecord(2, "SafetyIssue", "Safety issue")]);

        // Act
        var result = await CreateMockManager(mocks).GetDelayTagNamesAsync(UserId, CancellationToken.None);

        // Assert
        Assert.Equal(["Missing material", "Safety issue"], ResultAssert.Succeeded(result));
    }

    [Fact]
    public async Task GetDelayTagNamesAsync_InactiveUser_RefusesWithoutReadingTags()
    {
        // Arrange
        var mocks = new DelayInsightManagerMocks();
        mocks.UserAccessor.Setup(a => a.GetUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRecord(UserId, "Dana Foreman", "dana@example.com", null, IsActive: false));

        // Act
        var result = await CreateMockManager(mocks).GetDelayTagNamesAsync(UserId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        mocks.DelayAccessor.VerifyNoOtherCalls();
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

    private static void GrantAccess(DelayInsightManagerMocks mocks, int projectId)
    {
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(projectId, $"Project {projectId}", RoleNames.Foreman, []));
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
