using Accessors;
using Accessors.Delays;
using Accessors.Projects;
using Common.Delays;
using Common.Errors;
using Common.Projects;
using Common.Roles;
using Common.Users;
using DataContract.Delays;
using Managers;
using Managers.Delays;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Managers;

/// <summary>
/// The delay workflow writes: who may act, what state permits the action, and what reaches the
/// workflow Accessor. The Accessor itself is a double, so these tests prove no SQL behavior.
/// </summary>
public class DelayWorkflowManagerTests
{
    private const int UserId = 2;
    private const int ProjectId = 1;
    private const int DelayId = 7;
    private const int OwnerCrewId = 20;
    private const int ReporterCrewId = 30;
    private const int AffectedCrewId = 40;
    private const int ImpactId = 10;
    private const int CycleId = 5;
    private const int CurrentStatusId = 1;
    private const int TargetStatusId = 9;

    private readonly Mock<IProjectAccessAccessor> _access = new();
    private readonly Mock<IDelayWorkflowAccessor> _workflow = new();
    private DelayWorkflowWrite? _write;

    public DelayWorkflowManagerTests()
    {
        _workflow.Setup(a => a.GetStatusIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TargetStatusId);
        _workflow.Setup(a => a.GetCrewContactsAsync(ProjectId, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([UserId, 55]);
        _workflow.Setup(a => a.ApplyAsync(It.IsAny<DelayWorkflowWrite>(), It.IsAny<CancellationToken>()))
            .Callback<DelayWorkflowWrite, CancellationToken>((write, _) => _write = write)
            .ReturnsAsync(new DelayWorkflowResult(CycleId, 4900, 1));
    }

    [Fact]
    public async Task AcknowledgeAsync_ResponsibleSuperintendent_AcknowledgesCycleAndAppendsEvent()
    {
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Raised));
        var expectedReadyAt = new DateTimeOffset(2026, 10, 11, 19, 0, 0, TimeSpan.Zero);

        var result = await Manager().AcknowledgeAsync(UserId, ProjectId, DelayId,
            new AcknowledgeDelayRequest("WorkRemains", "Steel arrives at noon.", expectedReadyAt), CancellationToken.None);

        Assert.Equal((DelayStatusCodes.Acknowledged, DelayEventTypes.Acknowledged), (result.StatusCode, result.EventType));
        var write = Assert.IsType<DelayWorkflowWrite>(_write);
        Assert.Equal(TargetStatusId, write.ToStatusId);
        Assert.Equal(new DelayAcknowledgmentChange("WorkRemains", "Steel arrives at noon."), write.Cycle.Acknowledgment);
        Assert.Equal(CycleId, write.Cycle.CycleId);
        Assert.Equal(expectedReadyAt, write.NewExpectedReadyAt);
        // The actor never receives their own notice.
        Assert.Equal([55], write.RecipientUserIds);
    }

    [Fact]
    public async Task AcknowledgeAsync_ForemanOfOwnerCrew_RefusesBeforeAnyWrite()
    {
        GrantAccess(RoleNames.Foreman, Leads(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Raised));

        await Assert.ThrowsAsync<DelayActionNotPermittedException>(() => Manager().AcknowledgeAsync(
            UserId, ProjectId, DelayId, new AcknowledgeDelayRequest("WorkRemains", null, null), CancellationToken.None));

        AssertNothingWritten();
    }

    [Fact]
    public async Task AcknowledgeAsync_WithoutProjectAccess_RefusesBeforeReadingTheDelay()
    {
        _access.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectAccessRecord?)null);

        await Assert.ThrowsAsync<ProjectAccessDeniedException>(() => Manager().AcknowledgeAsync(
            UserId, ProjectId, DelayId, new AcknowledgeDelayRequest("WorkRemains", null, null), CancellationToken.None));

        _workflow.Verify(a => a.GetWorkflowStateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        AssertNothingWritten();
    }

    [Fact]
    public async Task ReviseCompletionTimeAsync_ResponsibleSuperintendent_KeepsTheCycleAndRecordsBothTimes()
    {
        var oldTime = new DateTimeOffset(2026, 10, 11, 19, 0, 0, TimeSpan.Zero);
        var newTime = oldTime.AddHours(3);
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Acknowledged) with { ExpectedReadyAt = oldTime });

        var result = await Manager().ReviseCompletionTimeAsync(UserId, ProjectId, DelayId,
            new ReviseCompletionTimeRequest(newTime), CancellationToken.None);

        // The status does not move, and the revision never opens another cycle.
        Assert.Equal((DelayStatusCodes.Acknowledged, DelayEventTypes.ExpectedReadyChanged), (result.StatusCode, result.EventType));
        var write = Assert.IsType<DelayWorkflowWrite>(_write);
        Assert.Null(write.ToStatusId);
        Assert.Equal(CycleId, write.Cycle.CycleId);
        Assert.Equal((oldTime, newTime), (write.OldExpectedReadyAt, write.NewExpectedReadyAt));
    }

    [Fact]
    public async Task VerifyHandoverAsync_AffectedCrewForeman_StoresTheCheckAgainstTheImpactAndCycle()
    {
        GrantAccess(RoleNames.Foreman, Leads(AffectedCrewId));
        SetState(StateOf(DelayStatusCodes.ReadyForHandover));

        var result = await Manager().VerifyHandoverAsync(UserId, ProjectId, DelayId,
            new VerifyHandoverRequest(ImpactId, "Bay is clear."), CancellationToken.None);

        Assert.Equal((DelayStatusCodes.ReadyForHandover, DelayEventTypes.HandoverVerified), (result.StatusCode, result.EventType));
        var write = Assert.IsType<DelayWorkflowWrite>(_write);
        Assert.Equal(new DelayVerificationChange(ImpactId, "Bay is clear."), write.Verification);
        Assert.Equal(CycleId, write.Cycle.CycleId);
        Assert.Null(write.ToStatusId);
    }

    [Fact]
    public async Task VerifyHandoverAsync_ForemanOfAnotherCrew_RefusesBeforeAnyWrite()
    {
        GrantAccess(RoleNames.Foreman, Leads(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.ReadyForHandover));

        await Assert.ThrowsAsync<DelayActionNotPermittedException>(() => Manager().VerifyHandoverAsync(
            UserId, ProjectId, DelayId, new VerifyHandoverRequest(ImpactId, null), CancellationToken.None));

        AssertNothingWritten();
    }

    [Fact]
    public async Task ResolveAsync_AffectedCrewHasNotVerified_RefusesBeforeAnyWrite()
    {
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.ReadyForHandover));

        await Assert.ThrowsAsync<DelayStateConflictException>(
            () => Manager().ResolveAsync(UserId, ProjectId, DelayId, CancellationToken.None));

        AssertNothingWritten();
    }

    [Fact]
    public async Task ResolveAsync_ReopenedCycleWithoutProofOfWorkPhotos_RefusesBeforeAnyWrite()
    {
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(VerifiedState() with { CycleCount = 2, HasProofOfWorkPhoto = false });

        await Assert.ThrowsAsync<DelayStateConflictException>(
            () => Manager().ResolveAsync(UserId, ProjectId, DelayId, CancellationToken.None));

        AssertNothingWritten();
    }

    [Fact]
    public async Task ResolveAsync_EveryCheckDone_ClosesTheCycleAndNotifiesBothCrews()
    {
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(VerifiedState());

        var result = await Manager().ResolveAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        Assert.Equal((DelayStatusCodes.Resolved, DelayEventTypes.Resolved), (result.StatusCode, result.EventType));
        var write = Assert.IsType<DelayWorkflowWrite>(_write);
        Assert.True(write.Cycle.Close);
        Assert.Equal(CycleId, write.Cycle.CycleId);
        _workflow.Verify(a => a.GetCrewContactsAsync(ProjectId,
            It.Is<IReadOnlyCollection<int>>(crews => crews.Contains(OwnerCrewId) && crews.Contains(AffectedCrewId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReopenAsync_ResolvedDelay_OpensANewCycleWithTheReason()
    {
        GrantAccess(RoleNames.Superintendent, Directs(AffectedCrewId));
        SetState(StateOf(DelayStatusCodes.Resolved) with { IsCycleOpen = false });

        var result = await Manager().ReopenAsync(UserId, ProjectId, DelayId,
            new ReopenDelayRequest("The steel is still missing."), CancellationToken.None);

        Assert.Equal((DelayStatusCodes.Reopened, DelayEventTypes.Reopened), (result.StatusCode, result.EventType));
        var write = Assert.IsType<DelayWorkflowWrite>(_write);
        Assert.Null(write.Cycle.CycleId);
        Assert.Equal("The steel is still missing.", write.Cycle.OpenReason);
    }

    [Fact]
    public async Task ReopenAsync_ActiveDelay_RefusesBeforeAnyWrite()
    {
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Acknowledged));

        await Assert.ThrowsAsync<DelayStateConflictException>(() => Manager().ReopenAsync(
            UserId, ProjectId, DelayId, new ReopenDelayRequest("Still blocked."), CancellationToken.None));

        AssertNothingWritten();
    }

    [Fact]
    public async Task RevokeAsync_SuperintendentOfTheReportingCrew_ClosesTheCycle()
    {
        GrantAccess(RoleNames.Superintendent, Directs(ReporterCrewId));
        SetState(StateOf(DelayStatusCodes.Raised));

        var result = await Manager().RevokeAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        Assert.Equal((DelayStatusCodes.Revoked, DelayEventTypes.Revoked), (result.StatusCode, result.EventType));
        var write = Assert.IsType<DelayWorkflowWrite>(_write);
        Assert.True(write.Cycle.Close);
    }

    [Fact]
    public async Task RevokeAsync_SuperintendentOfTheResponsibleCrewOnly_RefusesBeforeAnyWrite()
    {
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Raised));

        await Assert.ThrowsAsync<DelayActionNotPermittedException>(
            () => Manager().RevokeAsync(UserId, ProjectId, DelayId, CancellationToken.None));

        AssertNothingWritten();
    }

    [Fact]
    public async Task AcknowledgeAsync_BlankChoice_RefusesBeforeAnyRead()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Manager().AcknowledgeAsync(
            UserId, ProjectId, DelayId, new AcknowledgeDelayRequest("  ", null, null), CancellationToken.None));

        _access.VerifyNoOtherCalls();
        AssertNothingWritten();
    }

    private static DelayWorkflowState StateOf(string statusCode) =>
        new(DelayId, ProjectId, statusCode, CurrentStatusId, OwnerCrewId, ReporterCrewId, CycleId, true, false, 1,
            null, false, [new DelayWorkflowImpactRecord(ImpactId, AffectedCrewId, false)]);

    private static DelayWorkflowState VerifiedState() =>
        StateOf(DelayStatusCodes.ReadyForHandover) with
        {
            Impacts = [new DelayWorkflowImpactRecord(ImpactId, AffectedCrewId, true)],
        };

    private static CrewLeadRecord Directs(int crewId) => new(crewId, ProjectId, $"Crew {crewId}", false);

    private static CrewLeadRecord Leads(int crewId) => new(crewId, ProjectId, $"Crew {crewId}", true);

    private void GrantAccess(string roleName, params CrewLeadRecord[] crews) =>
        _access.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", roleName, crews));

    private void SetState(DelayWorkflowState state) =>
        _workflow.Setup(a => a.GetWorkflowStateAsync(ProjectId, DelayId, It.IsAny<CancellationToken>())).ReturnsAsync(state);

    private void AssertNothingWritten()
    {
        Assert.Null(_write);
        _workflow.Verify(a => a.ApplyAsync(It.IsAny<DelayWorkflowWrite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private IDelayManager Manager()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _access.Object);
        services.AddScoped(_ => _workflow.Object);
        services.AddScoped<AccessorFactory>();
        services.AddScoped<IDelayManager, DelayManager>();
        services.AddScoped<ManagerFactory>();

        return services.BuildServiceProvider().GetRequiredService<ManagerFactory>().CreateManager<IDelayManager>();
    }
}
