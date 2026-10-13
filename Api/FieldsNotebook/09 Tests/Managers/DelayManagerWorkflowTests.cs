using Accessors.Delays;
using Accessors.Projects;
using Common.Delays;
using Common.Errors;
using Common.Results;
using Common.Roles;
using DataContract.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Domain.Users;
using Managers.Delays;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Tests.Managers;

/// <summary>
/// The delay workflow writes: who may act, what state permits the action, and what reaches the
/// workflow Accessor. The Accessor itself is a double, so these tests prove no SQL behavior.
/// </summary>
public class DelayManagerWorkflowTests
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

    private readonly Mock<IProjectAccessAccessor> _access = new();
    private readonly Mock<IDelayWorkflowAccessor> _workflow = new();
    private IReadOnlyList<DelayWorkflowWrite>? _writes;

    public DelayManagerWorkflowTests()
    {
        _workflow.Setup(a => a.ApplyAsync(It.IsAny<IReadOnlyList<DelayWorkflowWrite>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<DelayWorkflowWrite>, CancellationToken>((writes, _) => _writes = writes)
            .ReturnsAsync(Result<DelayWorkflowResult>.Success(new DelayWorkflowResult(CycleId, 4900, 1)));
    }

    [Fact]
    public async Task AcknowledgeAsync_ResponsibleSuperintendent_AcknowledgesCycleAndAppendsEvent()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Raised));
        var expectedReadyAt = new DateTimeOffset(2026, 10, 11, 19, 0, 0, TimeSpan.Zero);
        var request = new AcknowledgeDelayRequest("WorkRemains", "Steel arrives at noon.", expectedReadyAt);

        // Act
        var result = await CreateManager().AcknowledgeAsync(UserId, ProjectId, DelayId, request, CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.Acknowledged, DelayEventTypes.Acknowledged), (response.StatusCode, response.EventType));
        var write = Assert.Single(_writes!);
        Assert.Equal((DelayStatusCodes.Raised, DelayStatusCodes.Acknowledged), (write.FromStatusCode, write.ToStatusCode));
        Assert.Equal(new DelayAcknowledgmentChange("WorkRemains", "Steel arrives at noon."), write.Cycle.Acknowledgment);
        Assert.Equal(CycleId, write.Cycle.CycleId);
        Assert.Equal(expectedReadyAt, write.NewExpectedReadyAt);
        Assert.Equal(UserId, write.ActorUserId);
        Assert.Equal([AffectedCrewId], write.RecipientCrewIds);
    }

    [Fact]
    public async Task AcknowledgeAsync_WorkIsAlreadyReady_AsksForTheHandoverCheckInTheSameTransaction()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Raised));
        var request = new AcknowledgeDelayRequest(DelayAcknowledgmentChoices.ReadyForHandover, null, null);

        // Act
        var result = await CreateManager().AcknowledgeAsync(UserId, ProjectId, DelayId, request, CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.ReadyForHandover, DelayEventTypes.ReadyForHandover), (response.StatusCode, response.EventType));
        _workflow.Verify(a => a.ApplyAsync(It.IsAny<IReadOnlyList<DelayWorkflowWrite>>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal([DelayEventTypes.Acknowledged, DelayEventTypes.ReadyForHandover], _writes!.Select(w => w.EventType));
        Assert.Equal(DelayStatusCodes.Acknowledged, _writes![1].FromStatusCode);
        Assert.Equal(CycleId, _writes[1].Cycle.CycleId);
        Assert.Null(_writes[1].Cycle.Acknowledgment);
    }

    [Fact]
    public async Task AcknowledgeAsync_ForemanOfOwnerCrew_RefusesBeforeAnyWrite()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, Leads(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Raised));
        var request = new AcknowledgeDelayRequest("WorkRemains", null, null);

        // Act
        var result = await CreateManager().AcknowledgeAsync(UserId, ProjectId, DelayId, request, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        AssertNothingWritten();
    }

    [Fact]
    public async Task AcknowledgeAsync_CycleAlreadyAcknowledged_RefusesBeforeAnyWrite()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Reopened) with { IsCycleAcknowledged = true });
        var request = new AcknowledgeDelayRequest("WorkRemains", null, null);

        // Act
        var result = await CreateManager().AcknowledgeAsync(UserId, ProjectId, DelayId, request, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Conflict, result);
        AssertNothingWritten();
    }

    [Fact]
    public async Task AcknowledgeAsync_WithoutProjectAccess_RefusesBeforeReadingTheDelay()
    {
        // Arrange
        var request = new AcknowledgeDelayRequest("WorkRemains", null, null);

        // Act
        var act = () => CreateManager().AcknowledgeAsync(UserId, ProjectId, DelayId, request, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        _workflow.Verify(a => a.GetWorkflowStateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        AssertNothingWritten();
    }

    [Fact]
    public async Task ReviseCompletionTimeAsync_ResponsibleSuperintendent_KeepsTheCycleAndRecordsBothTimes()
    {
        // Arrange
        var oldTime = new DateTimeOffset(2026, 10, 11, 19, 0, 0, TimeSpan.Zero);
        var newTime = oldTime.AddHours(3);
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Acknowledged) with { ExpectedReadyAt = oldTime });

        // Act
        var result = await CreateManager().ReviseCompletionTimeAsync(UserId, ProjectId, DelayId,
            new ReviseCompletionTimeRequest(newTime), CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.Acknowledged, DelayEventTypes.ExpectedReadyChanged), (response.StatusCode, response.EventType));
        var write = Assert.Single(_writes!);
        Assert.Null(write.ToStatusCode);
        Assert.Equal(DelayStatusCodes.Acknowledged, write.FromStatusCode);
        Assert.Equal(CycleId, write.Cycle.CycleId);
        Assert.Equal((oldTime, newTime), (write.OldExpectedReadyAt, write.NewExpectedReadyAt));
    }

    [Fact]
    public async Task ReviseCompletionTimeAsync_MissingTime_RefusesBeforeAnyWrite()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Acknowledged));

        // Act
        var result = await CreateManager().ReviseCompletionTimeAsync(UserId, ProjectId, DelayId,
            new ReviseCompletionTimeRequest(null), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.InvalidRequest, result);
        AssertNothingWritten();
    }

    [Fact]
    public async Task VerifyHandoverAsync_AffectedCrewForeman_StoresTheCheckAgainstTheImpactAndCycle()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, Leads(AffectedCrewId));
        SetState(StateOf(DelayStatusCodes.ReadyForHandover));

        // Act
        var result = await CreateManager().VerifyHandoverAsync(UserId, ProjectId, DelayId, ImpactId,
            new VerifyHandoverRequest("Bay is clear."), CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.ReadyForHandover, DelayEventTypes.HandoverVerified), (response.StatusCode, response.EventType));
        var write = Assert.Single(_writes!);
        Assert.Equal(new DelayVerificationChange(ImpactId, "Bay is clear."), write.Verification);
        Assert.Equal(CycleId, write.Cycle.CycleId);
        Assert.Null(write.ToStatusCode);
        Assert.Equal(DelayStatusCodes.ReadyForHandover, write.FromStatusCode);
        Assert.Equal([OwnerCrewId], write.RecipientCrewIds);
    }

    [Fact]
    public async Task VerifyHandoverAsync_ForemanOfAnotherCrew_RefusesBeforeAnyWrite()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, Leads(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.ReadyForHandover));

        // Act
        var result = await CreateManager().VerifyHandoverAsync(UserId, ProjectId, DelayId, ImpactId,
            new VerifyHandoverRequest(null), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        AssertNothingWritten();
    }

    [Fact]
    public async Task VerifyHandoverAsync_ImpactNotOnTheDelay_ReturnsNotFoundWithoutWriting()
    {
        // Arrange
        GrantAccess(RoleNames.Foreman, Leads(AffectedCrewId));
        SetState(StateOf(DelayStatusCodes.ReadyForHandover));

        // Act
        var result = await CreateManager().VerifyHandoverAsync(UserId, ProjectId, DelayId, ImpactId + 1,
            new VerifyHandoverRequest(null), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
        AssertNothingWritten();
    }

    [Fact]
    public async Task ResolveAsync_AffectedCrewHasNotVerified_RefusesBeforeAnyWrite()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.ReadyForHandover));

        // Act
        var result = await CreateManager().ResolveAsync(UserId, ProjectId, DelayId, ProofOfWork(), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Conflict, result);
        AssertNothingWritten();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ResolveAsync_WithoutProofOfWorkPhotos_RefusesBeforeAnyWrite(int cycleCount)
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(VerifiedState() with { CycleCount = cycleCount });

        // Act
        var result = await CreateManager().ResolveAsync(UserId, ProjectId, DelayId, new ResolveDelayRequest(), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.InvalidRequest, result);
        AssertNothingWritten();
    }

    [Fact]
    public async Task ResolveAsync_ProofOfWorkIsNotAnImage_RefusesBeforeAnyWrite()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(VerifiedState());
        var request = ProofOfWork();
        request.Photos[0].File = Photo("notes.txt", "text/plain");

        // Act
        var result = await CreateManager().ResolveAsync(UserId, ProjectId, DelayId, request, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.InvalidRequest, result);
        AssertNothingWritten();
    }

    [Fact]
    public async Task ResolveAsync_EveryCheckDone_ClosesTheCycleAndNotifiesBothCrews()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(VerifiedState());

        // Act
        var result = await CreateManager().ResolveAsync(UserId, ProjectId, DelayId, ProofOfWork(), CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.Resolved, DelayEventTypes.Resolved), (response.StatusCode, response.EventType));
        var write = Assert.Single(_writes!);
        Assert.True(write.Cycle.Close);
        Assert.Equal(CycleId, write.Cycle.CycleId);
        var photo = Assert.Single(write.ProofOfWorkPhotos);
        Assert.Equal(("fixed.jpg", "image/jpeg"), (photo.FileName, photo.ContentType));
        Assert.Equal([AffectedCrewId, OwnerCrewId], write.RecipientCrewIds);
    }

    [Fact]
    public async Task ReopenAsync_ResolvedDelay_OpensANewCycleWithTheReason()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(AffectedCrewId));
        SetState(StateOf(DelayStatusCodes.Resolved) with { IsCycleOpen = false });

        // Act
        var result = await CreateManager().ReopenAsync(UserId, ProjectId, DelayId,
            new ReopenDelayRequest("The steel is still missing."), CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.Reopened, DelayEventTypes.Reopened), (response.StatusCode, response.EventType));
        var write = Assert.Single(_writes!);
        Assert.Null(write.Cycle.CycleId);
        Assert.Equal("The steel is still missing.", write.Cycle.OpenReason);
    }

    [Fact]
    public async Task ReopenAsync_ActiveDelay_RefusesBeforeAnyWrite()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Acknowledged));

        // Act
        var result = await CreateManager().ReopenAsync(UserId, ProjectId, DelayId,
            new ReopenDelayRequest("Still blocked."), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Conflict, result);
        AssertNothingWritten();
    }

    [Fact]
    public async Task RevokeAsync_SuperintendentOfTheReportingCrew_ClosesTheCycle()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(ReporterCrewId));
        SetState(StateOf(DelayStatusCodes.Raised));

        // Act
        var result = await CreateManager().RevokeAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.Revoked, DelayEventTypes.Revoked), (response.StatusCode, response.EventType));
        Assert.True(Assert.Single(_writes!).Cycle.Close);
    }

    [Fact]
    public async Task RevokeAsync_SuperintendentOfTheResponsibleCrewOnly_RefusesBeforeAnyWrite()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(StateOf(DelayStatusCodes.Raised));

        // Act
        var result = await CreateManager().RevokeAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        AssertNothingWritten();
    }

    [Fact]
    public async Task RevokeAsync_DelayMissing_ReturnsNotFoundWithoutWriting()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(ReporterCrewId));

        // Act
        var result = await CreateManager().RevokeAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
        AssertNothingWritten();
    }

    [Fact]
    public async Task RevokeAsync_AnotherActionMovedTheDelayFirst_ReturnsTheConflict()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(ReporterCrewId));
        SetState(StateOf(DelayStatusCodes.Raised));
        _workflow.Setup(a => a.ApplyAsync(It.IsAny<IReadOnlyList<DelayWorkflowWrite>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DelayWorkflowResult>.Failure(DelayErrors.StateConflict(DelayId, "another action changed its status first.")));

        // Act
        var result = await CreateManager().RevokeAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Conflict, result);
    }

    [Fact]
    public async Task RevokeAsync_AccessorFails_PropagatesServerFailure()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent, Directs(ReporterCrewId));
        SetState(StateOf(DelayStatusCodes.Raised));
        _workflow.Setup(a => a.ApplyAsync(It.IsAny<IReadOnlyList<DelayWorkflowWrite>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Required delay status Revoked is not configured."));
        var manager = CreateManager();

        // Act
        var revoke = () => manager.RevokeAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(revoke);
    }

    #region Private Methods

    private static DelayWorkflowState StateOf(string statusCode)
    {
        return new DelayWorkflowState(DelayId, ProjectId, statusCode, CurrentStatusId, OwnerCrewId, ReporterCrewId, CycleId, true, false, 1,
            null, [new DelayWorkflowImpactRecord(ImpactId, AffectedCrewId, false)]);
    }

    private static DelayWorkflowState VerifiedState()
    {
        return StateOf(DelayStatusCodes.ReadyForHandover) with
        {
            Impacts = [new DelayWorkflowImpactRecord(ImpactId, AffectedCrewId, true)],
        };
    }

    private static ResolveDelayRequest ProofOfWork()
    {
        return new ResolveDelayRequest
        {
            Photos = [new() { File = Photo("fixed.jpg", "image/jpeg"), Thumbnail = Photo("thumbnail.jpg", "image/jpeg") }],
        };
    }

    private static FormFile Photo(string name, string contentType)
    {
        return new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "photo", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    private static CrewLeadRecord Directs(int crewId)
    {
        return new CrewLeadRecord(crewId, ProjectId, $"Crew {crewId}", false);
    }

    private static CrewLeadRecord Leads(int crewId)
    {
        return new CrewLeadRecord(crewId, ProjectId, $"Crew {crewId}", true);
    }

    private void GrantAccess(string roleName, params CrewLeadRecord[] crews)
    {
        _access.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", roleName, crews));
    }

    private void SetState(DelayWorkflowState state)
    {
        _workflow.Setup(a => a.GetWorkflowStateAsync(ProjectId, DelayId, It.IsAny<CancellationToken>())).ReturnsAsync(state);
    }

    private void AssertNothingWritten()
    {
        Assert.Null(_writes);
        _workflow.Verify(a => a.ApplyAsync(It.IsAny<IReadOnlyList<DelayWorkflowWrite>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private DelayManager CreateManager()
    {
        return new ManagerTestHost().With(_access).With(_workflow).Create<DelayManager>();
    }

    #endregion
}
