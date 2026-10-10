using Accessors;
using Accessors.Delays;
using Accessors.Projects;
using Common.Delays;
using Common.Projects;
using Common.Roles;
using Managers;
using Managers.Delays;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Managers;

public class DelayManagerTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;
    private readonly Mock<IProjectAccessAccessor> _access = new();
    private readonly Mock<IDelayAccessor> _delays = new();

    [Fact]
    public async Task GetProjectTrackingDetailsAsync_ZeroProjectId_RejectsBeforeAccessorReads()
    {
        using var services = CreateServices();
        var manager = services.GetRequiredService<ManagerFactory>().CreateManager<IDelayManager>();

        var error = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => manager.GetProjectTrackingDetailsAsync(UserId, 0, CancellationToken.None));

        Assert.Equal("projectId", error.ParamName);
        _access.VerifyNoOtherCalls();
        _delays.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetProjectTrackingDetailsAsync_ProjectWithData_ReturnsTrackingAndEveryAffectedCrew()
    {
        GrantAccess();
        var timestamp = DateTimeOffset.UnixEpoch;
        var summary = new ActiveDelayRecord(7, "FN-007", "Steel blocker", "Missing steel", "Reopened",
            "MissingMaterial", "Missing material", 2, "Steel", "AfterArrival", "Bay 2", "Bay 2",
            null, null, null, timestamp, 2, true, timestamp.AddDays(1));
        var data = new TrackingSet(
            [new TrackingDelayRecord(summary, UserId, "Reporter", "Welding")],
            [new TrackingImpactRecord(10, 7, "Welding", 9, "Weld frame"), new TrackingImpactRecord(11, 7, "Coatings", null, null)],
            [new TrackingCycleRecord(1, 7, timestamp, null, null, null, null, null, null, timestamp.AddHours(1)),
             new TrackingCycleRecord(2, 7, timestamp.AddHours(2), "Still blocked", null, null, null, null, timestamp.AddDays(1), null)],
            [new TrackingVerificationRecord(10, 1, "Old verifier", timestamp, null)], [], [], []);
        using var cancellation = new CancellationTokenSource();
        _delays.Setup(a => a.GetProjectTrackingDetailsAsync(ProjectId, cancellation.Token)).ReturnsAsync(data);
        using var services = CreateServices();

        var result = await services.GetRequiredService<ManagerFactory>().CreateManager<IDelayManager>()
            .GetProjectTrackingDetailsAsync(UserId, ProjectId, cancellation.Token);

        Assert.Equal((ProjectId, "Omaha Fabrication"), (result.ProjectId, result.ProjectName));
        var delay = Assert.Single(result.Delays);
        Assert.Equal("FN-007", delay.Summary.Number);
        Assert.True(delay.Summary.IsHighPriority);
        Assert.Equal(["Welding", "Coatings"], delay.Summary.BlockedCrewNames);
        Assert.Equal(2, delay.Cycles.Count);
        Assert.All(delay.Impacts, impact => Assert.Empty(impact.Verifications));
        _delays.Verify(a => a.GetProjectTrackingDetailsAsync(ProjectId, cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task GetProjectTrackingDetailsAsync_ProjectWithoutData_ReturnsProjectNameAndEmptyTracking()
    {
        GrantAccess();
        _delays.Setup(a => a.GetProjectTrackingDetailsAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(TrackingSet.Empty);
        using var services = CreateServices();

        var result = await services.GetRequiredService<ManagerFactory>().CreateManager<IDelayManager>()
            .GetProjectTrackingDetailsAsync(UserId, ProjectId, CancellationToken.None);

        Assert.Empty(result.Delays);
        Assert.Equal(ProjectId, result.ProjectId);
        Assert.Equal("Omaha Fabrication", result.ProjectName);
        _delays.Verify(a => a.GetProjectTrackingDetailsAsync(ProjectId, CancellationToken.None), Times.Once);
    }

    private void GrantAccess() =>
        _access.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", RoleNames.Foreman, []));

    private ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _access.Object);
        services.AddScoped(_ => _delays.Object);
        services.AddScoped<AccessorFactory>();
        services.AddScoped<IDelayManager, DelayManager>();
        services.AddScoped<ManagerFactory>();
        return services.BuildServiceProvider();
    }
}