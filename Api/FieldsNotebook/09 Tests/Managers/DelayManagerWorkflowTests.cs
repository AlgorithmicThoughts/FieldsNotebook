using Accessors;
using Accessors.Delays;
using Accessors.Projects;
using Accessors.Speech;
using Accessors.WorkItems;
using Common.Delays;
using Common.Errors;
using Common.Results;
using Common.Roles;
using DataContract.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Domain.Users;
using Engines;
using Engines.Delays;
using Engines.Authorization;
using Managers.Delays;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities;
using Utilities.OperationContract;

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
    private const int OwnerContactId = 21;
    private const int AffectedForemanId = 41;
    private const int AffectedSuperintendentId = 42;

    [Fact]
    public async Task AcknowledgeAsync_ResponsibleSuperintendent_AcknowledgesCycleAndAppendsEvent()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.Raised));
        // The actor is also an owner crew contact, so the notices prove they are left out.
        ProvideCrewContacts(mocks, new(OwnerCrewId, UserId), new(OwnerCrewId, OwnerContactId),
            new(AffectedCrewId, AffectedForemanId), new(AffectedCrewId, AffectedSuperintendentId));
        var applied = AcceptWrites(mocks, new DelayWorkflowResult(CycleId, 4900, 2));
        var expectedReadyAt = new DateTimeOffset(2026, 10, 11, 19, 0, 0, TimeSpan.Zero);
        var request = new AcknowledgeDelayRequest("WorkRemains", "Steel arrives at noon.", expectedReadyAt);

        // Act
        var result = await CreateMockManager(mocks).AcknowledgeAsync(UserId, ProjectId, DelayId, request, CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.Acknowledged, DelayEventTypes.Acknowledged), (response.StatusCode, response.EventType));
        var write = Assert.Single(Assert.Single(applied).Writes);
        Assert.Equal((DelayStatusCodes.Raised, DelayStatusCodes.Acknowledged), (write.FromStatusCode, write.ToStatusCode));
        Assert.Equal(new DelayAcknowledgmentChange("WorkRemains", "Steel arrives at noon."), write.Cycle.Acknowledgment);
        Assert.Equal(CycleId, write.Cycle.CycleId);
        Assert.Equal(expectedReadyAt, write.NewExpectedReadyAt);
        Assert.Equal(UserId, write.ActorUserId);
        Assert.Equal([AffectedForemanId, AffectedSuperintendentId], write.RecipientUserIds);
    }

    [Fact]
    public async Task AcknowledgeAsync_WorkIsAlreadyReady_AsksForTheHandoverCheckInTheSameTransaction()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.Raised));
        ProvideCrewContacts(mocks, new(OwnerCrewId, OwnerContactId), new(AffectedCrewId, AffectedForemanId));
        var applied = AcceptWrites(mocks, new DelayWorkflowResult(CycleId, 4901, 0));
        var request = new AcknowledgeDelayRequest(DelayAcknowledgmentChoices.ReadyForHandover, null, null);

        // Act
        var result = await CreateMockManager(mocks).AcknowledgeAsync(UserId, ProjectId, DelayId, request, CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.ReadyForHandover, DelayEventTypes.ReadyForHandover), (response.StatusCode, response.EventType));
        var writes = Assert.Single(applied).Writes;
        Assert.Equal([DelayEventTypes.Acknowledged, DelayEventTypes.ReadyForHandover], writes.Select(w => w.EventType));
        Assert.Equal(DelayStatusCodes.Acknowledged, writes[1].FromStatusCode);
        Assert.Equal(CycleId, writes[1].Cycle.CycleId);
        Assert.Null(writes[1].Cycle.Acknowledgment);
    }

    [Fact]
    public async Task AcknowledgeAsync_ForemanOfOwnerCrew_RefusesBeforeAnyWrite()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman, Leads(OwnerCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.Raised));
        var request = new AcknowledgeDelayRequest("WorkRemains", null, null);

        // Act
        var result = await CreateMockManager(mocks).AcknowledgeAsync(UserId, ProjectId, DelayId, request, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task AcknowledgeAsync_CycleAlreadyAcknowledged_RefusesBeforeAnyWrite()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.Reopened) with { IsCycleAcknowledged = true });
        var request = new AcknowledgeDelayRequest("WorkRemains", null, null);

        // Act
        var result = await CreateMockManager(mocks).AcknowledgeAsync(UserId, ProjectId, DelayId, request, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Conflict, result);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task AcknowledgeAsync_WithoutProjectAccess_RefusesBeforeReadingTheDelay()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectAccessRecord?)null);
        var request = new AcknowledgeDelayRequest("WorkRemains", null, null);

        // Act
        var act = () => CreateMockManager(mocks).AcknowledgeAsync(UserId, ProjectId, DelayId, request, CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        mocks.DelayWorkflowAccessor.Verify(a => a.GetWorkflowStateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task ReviseCompletionTimeAsync_ResponsibleSuperintendent_KeepsTheCycleAndRecordsBothTimes()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        var oldTime = new DateTimeOffset(2026, 10, 11, 19, 0, 0, TimeSpan.Zero);
        var newTime = oldTime.AddHours(3);
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.Acknowledged) with { ExpectedReadyAt = oldTime });
        ProvideCrewContacts(mocks, new(OwnerCrewId, OwnerContactId), new(AffectedCrewId, AffectedForemanId));
        var applied = AcceptWrites(mocks, new DelayWorkflowResult(CycleId, 4902, 0));

        // Act
        var result = await CreateMockManager(mocks).ReviseCompletionTimeAsync(UserId, ProjectId, DelayId,
            new ReviseCompletionTimeRequest(newTime), CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.Acknowledged, DelayEventTypes.ExpectedReadyChanged), (response.StatusCode, response.EventType));
        var write = Assert.Single(Assert.Single(applied).Writes);
        Assert.Null(write.ToStatusCode);
        Assert.Equal(DelayStatusCodes.Acknowledged, write.FromStatusCode);
        Assert.Equal(CycleId, write.Cycle.CycleId);
        Assert.Equal((oldTime, newTime), (write.OldExpectedReadyAt, write.NewExpectedReadyAt));
    }

    [Fact]
    public async Task ReviseCompletionTimeAsync_MissingTime_RefusesBeforeAnyWrite()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.Acknowledged));

        // Act
        var result = await CreateMockManager(mocks).ReviseCompletionTimeAsync(UserId, ProjectId, DelayId,
            new ReviseCompletionTimeRequest(null), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.InvalidRequest, result);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task VerifyHandoverAsync_AffectedCrewForeman_StoresTheCheckAgainstTheImpactAndCycle()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman, Leads(AffectedCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.ReadyForHandover));
        // The actor is an owner crew contact too, so only the other owner contact hears of the check.
        ProvideCrewContacts(mocks, new(OwnerCrewId, UserId), new(OwnerCrewId, OwnerContactId), new(AffectedCrewId, AffectedForemanId));
        var applied = AcceptWrites(mocks, new DelayWorkflowResult(CycleId, 4903, 1));

        // Act
        var result = await CreateMockManager(mocks).VerifyHandoverAsync(UserId, ProjectId, DelayId, ImpactId,
            new VerifyHandoverRequest("Bay is clear."), CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.ReadyForHandover, DelayEventTypes.HandoverVerified), (response.StatusCode, response.EventType));
        var write = Assert.Single(Assert.Single(applied).Writes);
        Assert.Equal(new DelayVerificationChange(ImpactId, "Bay is clear."), write.Verification);
        Assert.Equal(CycleId, write.Cycle.CycleId);
        Assert.Null(write.ToStatusCode);
        Assert.Equal(DelayStatusCodes.ReadyForHandover, write.FromStatusCode);
        Assert.Equal([OwnerContactId], write.RecipientUserIds);
    }

    [Fact]
    public async Task VerifyHandoverAsync_ForemanOfAnotherCrew_RefusesBeforeAnyWrite()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman, Leads(OwnerCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.ReadyForHandover));

        // Act
        var result = await CreateMockManager(mocks).VerifyHandoverAsync(UserId, ProjectId, DelayId, ImpactId,
            new VerifyHandoverRequest(null), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task VerifyHandoverAsync_ImpactNotOnTheDelay_ReturnsNotFoundWithoutWriting()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman, Leads(AffectedCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.ReadyForHandover));

        // Act
        var result = await CreateMockManager(mocks).VerifyHandoverAsync(UserId, ProjectId, DelayId, ImpactId + 1,
            new VerifyHandoverRequest(null), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task ResolveAsync_AffectedCrewHasNotVerified_RefusesBeforeAnyWrite()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.ReadyForHandover));

        // Act
        var result = await CreateMockManager(mocks).ResolveAsync(UserId, ProjectId, DelayId, ProofOfWork(), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Conflict, result);
        AssertNothingWritten(mocks);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ResolveAsync_WithoutProofOfWorkPhotos_RefusesBeforeAnyWrite(int cycleCount)
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, VerifiedState() with { CycleCount = cycleCount });

        // Act
        var result = await CreateMockManager(mocks).ResolveAsync(UserId, ProjectId, DelayId, new ResolveDelayRequest(), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.InvalidRequest, result);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task ResolveAsync_ProofOfWorkIsNotAnImage_RefusesBeforeAnyWrite()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, VerifiedState());
        var request = ProofOfWork();
        request.Photos[0].File = Photo("notes.txt", "text/plain");

        // Act
        var result = await CreateMockManager(mocks).ResolveAsync(UserId, ProjectId, DelayId, request, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.InvalidRequest, result);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task ResolveAsync_EveryCheckDone_ClosesTheCycleAndNotifiesBothCrews()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        var resolvedAt = new DateTimeOffset(2026, 10, 12, 15, 30, 0, TimeSpan.Zero);
        mocks.ClockUtility.Setup(c => c.UtcNow).Returns(resolvedAt);
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, VerifiedState());
        ProvideCrewContacts(mocks, new(OwnerCrewId, UserId), new(OwnerCrewId, OwnerContactId),
            new(AffectedCrewId, AffectedForemanId), new(AffectedCrewId, AffectedSuperintendentId));
        StoreProofOfWork(mocks);
        var applied = AcceptWrites(mocks, new DelayWorkflowResult(CycleId, 4904, 3));

        // Act
        var result = await CreateMockManager(mocks).ResolveAsync(UserId, ProjectId, DelayId, ProofOfWork(), CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.Resolved, DelayEventTypes.Resolved), (response.StatusCode, response.EventType));
        var (writes, recordedPhotos) = Assert.Single(applied);
        var write = Assert.Single(writes);
        Assert.True(write.Cycle.Close);
        Assert.Equal((CycleId, resolvedAt), (write.Cycle.CycleId, write.OccurredAt));
        var photo = Assert.Single(write.ProofOfWorkPhotos);
        Assert.Equal(("fixed.jpg", "image/jpeg"), (photo.FileName, photo.ContentType));
        Assert.Equal([OwnerContactId, AffectedForemanId, AffectedSuperintendentId], write.RecipientUserIds);
        mocks.DelayPhotoAccessor.Verify(a => a.StoreProofOfWorkPhotosAsync(ProjectId, DelayId, CycleId, resolvedAt,
            It.IsAny<IReadOnlyList<ReportPhotoUploadRecord>>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("stored/fixed.jpg", Assert.Single(recordedPhotos).OriginalUrl);
        mocks.DelayPhotoAccessor.Verify(a => a.RemoveAsync(It.IsAny<StoredPhotoSet>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_AnotherActionMovedTheDelayFirst_RemovesTheStoredProofOfWork()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, VerifiedState());
        ProvideCrewContacts(mocks, new(OwnerCrewId, OwnerContactId), new(AffectedCrewId, AffectedForemanId));
        StoreProofOfWork(mocks);
        mocks.DelayWorkflowAccessor
            .Setup(a => a.ApplyAsync(It.IsAny<IReadOnlyList<DelayWorkflowWrite>>(), It.IsAny<IReadOnlyList<ReportPhotoRecord>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DelayWorkflowResult>.Failure(DelayErrors.StateConflict(DelayId, "another action changed its status first.")));

        // Act
        var result = await CreateMockManager(mocks).ResolveAsync(UserId, ProjectId, DelayId, ProofOfWork(), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Conflict, result);
        mocks.DelayPhotoAccessor.Verify(a => a.RemoveAsync(It.Is<StoredPhotoSet>(p => p.StorageKeys.Single() == "stored/fixed.jpg")), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_AccessorFails_RemovesTheStoredProofOfWorkAndPropagatesTheFailure()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, VerifiedState());
        ProvideCrewContacts(mocks, new(OwnerCrewId, OwnerContactId), new(AffectedCrewId, AffectedForemanId));
        StoreProofOfWork(mocks);
        mocks.DelayWorkflowAccessor
            .Setup(a => a.ApplyAsync(It.IsAny<IReadOnlyList<DelayWorkflowWrite>>(), It.IsAny<IReadOnlyList<ReportPhotoRecord>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Save failed"));
        var manager = CreateMockManager(mocks);

        // Act
        var resolve = () => manager.ResolveAsync(UserId, ProjectId, DelayId, ProofOfWork(), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(resolve);
        mocks.DelayPhotoAccessor.Verify(a => a.RemoveAsync(It.Is<StoredPhotoSet>(p => p.StorageKeys.Single() == "stored/fixed.jpg")), Times.Once);
    }

    [Fact]
    public async Task ReopenAsync_ResolvedDelay_OpensANewCycleWithTheReason()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(AffectedCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.Resolved) with { IsCycleOpen = false });
        ProvideCrewContacts(mocks, new(OwnerCrewId, OwnerContactId), new(AffectedCrewId, AffectedForemanId));
        var applied = AcceptWrites(mocks, new DelayWorkflowResult(CycleId + 1, 4905, 2));

        // Act
        var result = await CreateMockManager(mocks).ReopenAsync(UserId, ProjectId, DelayId,
            new ReopenDelayRequest("The steel is still missing."), CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.Reopened, DelayEventTypes.Reopened, CycleId + 1), (response.StatusCode, response.EventType, response.CycleId));
        var write = Assert.Single(Assert.Single(applied).Writes);
        Assert.Null(write.Cycle.CycleId);
        Assert.Equal("The steel is still missing.", write.Cycle.OpenReason);
    }

    [Fact]
    public async Task ReopenAsync_ActiveDelay_RefusesBeforeAnyWrite()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.Acknowledged));

        // Act
        var result = await CreateMockManager(mocks).ReopenAsync(UserId, ProjectId, DelayId,
            new ReopenDelayRequest("Still blocked."), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Conflict, result);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task RevokeAsync_SuperintendentOfTheReportingCrew_ClosesTheCycle()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(ReporterCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.Raised));
        ProvideCrewContacts(mocks, new(OwnerCrewId, OwnerContactId), new(AffectedCrewId, AffectedForemanId));
        var applied = AcceptWrites(mocks, new DelayWorkflowResult(CycleId, 4906, 2));

        // Act
        var result = await CreateMockManager(mocks).RevokeAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        var response = ResultAssert.Succeeded(result);
        Assert.Equal((DelayStatusCodes.Revoked, DelayEventTypes.Revoked), (response.StatusCode, response.EventType));
        Assert.True(Assert.Single(Assert.Single(applied).Writes).Cycle.Close);
    }

    [Fact]
    public async Task RevokeAsync_SuperintendentOfTheResponsibleCrewOnly_RefusesBeforeAnyWrite()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(OwnerCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.Raised));

        // Act
        var result = await CreateMockManager(mocks).RevokeAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task RevokeAsync_DelayMissing_ReturnsNotFoundWithoutWriting()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(ReporterCrewId));
        mocks.DelayWorkflowAccessor.Setup(a => a.GetWorkflowStateAsync(ProjectId, DelayId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DelayWorkflowState?)null);

        // Act
        var result = await CreateMockManager(mocks).RevokeAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.NotFound, result);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task RevokeAsync_AnotherActionMovedTheDelayFirst_ReturnsTheConflict()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(ReporterCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.Raised));
        ProvideCrewContacts(mocks, new(OwnerCrewId, OwnerContactId), new(AffectedCrewId, AffectedForemanId));
        mocks.DelayWorkflowAccessor
            .Setup(a => a.ApplyAsync(It.IsAny<IReadOnlyList<DelayWorkflowWrite>>(), It.IsAny<IReadOnlyList<ReportPhotoRecord>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DelayWorkflowResult>.Failure(DelayErrors.StateConflict(DelayId, "another action changed its status first.")));

        // Act
        var result = await CreateMockManager(mocks).RevokeAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Conflict, result);
    }

    [Fact]
    public async Task RevokeAsync_AccessorFails_PropagatesServerFailure()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent, Directs(ReporterCrewId));
        SetState(mocks, StateOf(DelayStatusCodes.Raised));
        ProvideCrewContacts(mocks, new(OwnerCrewId, OwnerContactId), new(AffectedCrewId, AffectedForemanId));
        mocks.DelayWorkflowAccessor
            .Setup(a => a.ApplyAsync(It.IsAny<IReadOnlyList<DelayWorkflowWrite>>(), It.IsAny<IReadOnlyList<ReportPhotoRecord>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Required delay status Revoked is not configured."));
        var manager = CreateMockManager(mocks);

        // Act
        var revoke = () => manager.RevokeAsync(UserId, ProjectId, DelayId, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(revoke);
    }

    #region Private Test Helper

    /// <summary>A DelayManager over the real Engines it decides with and the mocked Accessors and Utilities it needs.</summary>
    private static DelayManager CreateMockManager(DelayManagerMocks mocks)
    {
        var services = new ServiceCollection();
        services.AddSingleton<AccessorFactory>();
        services.AddSingleton<EngineFactory>();
        services.AddSingleton<IAuthorizationEngine, AuthorizationEngine>();
        services.AddSingleton<UtilityFactory>();
        services.AddSingleton<IDelayWorkflowEngine, DelayWorkflowEngine>();
        services.AddSingleton<IDelaySpeechEngine, DelaySpeechEngine>();
        mocks.RegistryOverride(services);

        return ActivatorUtilities.CreateInstance<DelayManager>(services.BuildServiceProvider());
    }

    private static void GrantAccess(DelayManagerMocks mocks, string roleName, params CrewLeadRecord[] crews)
    {
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Omaha Fabrication", roleName, crews));
    }

    private static void SetState(DelayManagerMocks mocks, DelayWorkflowState state)
    {
        mocks.DelayWorkflowAccessor.Setup(a => a.GetWorkflowStateAsync(ProjectId, DelayId, It.IsAny<CancellationToken>())).ReturnsAsync(state);
    }

    private static void ProvideCrewContacts(DelayManagerMocks mocks, params CrewContactRecord[] contacts)
    {
        mocks.ProjectAccessor.Setup(a => a.GetCrewContactsAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(contacts);
    }

    /// <summary>The photo Accessor stores every proof-of-work upload under its file name.</summary>
    private static void StoreProofOfWork(DelayManagerMocks mocks)
    {
        mocks.DelayPhotoAccessor
            .Setup(a => a.StoreProofOfWorkPhotosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<IReadOnlyList<ReportPhotoUploadRecord>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, int _, int _, DateTimeOffset _, IReadOnlyList<ReportPhotoUploadRecord> uploads, CancellationToken _) => new StoredPhotoSet(
                uploads.Select(u => new ReportPhotoRecord($"stored/{u.FileName}", $"stored/{u.FileName}.thumbnail", u.FileName, u.Latitude, u.Longitude)).ToList(),
                uploads.Select(u => $"stored/{u.FileName}").ToList()));
    }

    /// <summary>The workflow Accessor commits every action as <paramref name="result"/>; each call's writes and photos are captured.</summary>
    private static List<(IReadOnlyList<DelayWorkflowWrite> Writes, IReadOnlyList<ReportPhotoRecord> Photos)> AcceptWrites(
        DelayManagerMocks mocks, DelayWorkflowResult result)
    {
        var applied = new List<(IReadOnlyList<DelayWorkflowWrite>, IReadOnlyList<ReportPhotoRecord>)>();
        mocks.DelayWorkflowAccessor
            .Setup(a => a.ApplyAsync(It.IsAny<IReadOnlyList<DelayWorkflowWrite>>(), It.IsAny<IReadOnlyList<ReportPhotoRecord>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<DelayWorkflowWrite>, IReadOnlyList<ReportPhotoRecord>, CancellationToken>((writes, photos, _) => applied.Add((writes, photos)))
            .ReturnsAsync(Result<DelayWorkflowResult>.Success(result));
        return applied;
    }

    private static void AssertNothingWritten(DelayManagerMocks mocks)
    {
        mocks.DelayWorkflowAccessor.Verify(
            a => a.ApplyAsync(It.IsAny<IReadOnlyList<DelayWorkflowWrite>>(), It.IsAny<IReadOnlyList<ReportPhotoRecord>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mocks.DelayPhotoAccessor.Verify(a => a.StoreProofOfWorkPhotosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<IReadOnlyList<ReportPhotoUploadRecord>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

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

    /// <summary>The Accessors and Utilities DelayManager resolves, each a mock the test arranges for itself.</summary>
    private sealed class DelayManagerMocks
    {
        public Mock<IProjectAccessor> ProjectAccessor { get; } = new();
        public Mock<IWorkItemAccessor> WorkItemAccessor { get; } = new();
        public Mock<IDelayAccessor> DelayAccessor { get; } = new();
        public Mock<IDelayWorkflowAccessor> DelayWorkflowAccessor { get; } = new();
        public Mock<IDelayPhotoAccessor> DelayPhotoAccessor { get; } = new();
        public Mock<ISpeechAccessor> SpeechAccessor { get; } = new();
        public Mock<IClockUtility> ClockUtility { get; } = new();
        public Mock<ILogger<AuthorizationEngine>> Logger { get; } = new();

        /// <summary>Registers each mock in place of the concrete class the factories would otherwise resolve.</summary>
        public void RegistryOverride(IServiceCollection services)
        {
            services.AddSingleton(ProjectAccessor.Object);
            services.AddSingleton(WorkItemAccessor.Object);
            services.AddSingleton(DelayAccessor.Object);
            services.AddSingleton(DelayWorkflowAccessor.Object);
            services.AddSingleton(DelayPhotoAccessor.Object);
            services.AddSingleton(SpeechAccessor.Object);
            services.AddSingleton(ClockUtility.Object);
            services.AddSingleton(Logger.Object);
        }
    }

    #endregion
}
