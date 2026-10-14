using Accessors;
using Accessors.Delays;
using Accessors.Projects;
using Accessors.Speech;
using Accessors.WorkItems;
using Common.Delays;
using Common.Errors;
using Common.Roles;
using DataContract.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Home;
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

/// <summary>Raising a delay, the report form behind it, and the spoken shortcut that fills that form.</summary>
public class DelayManagerRaiseTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;

    [Theory]
    [InlineData(DelayReportTypes.AfterArrival, new int[] { 10 }, DelayEventTypes.Raised)]
    [InlineData(DelayReportTypes.AdvanceNotice, new int[] { 10, 11, 12 }, DelayEventTypes.AdvanceNoticeRaised)]
    public async Task RaiseAsync_ValidReport_PreservesAllImpactsPhotosPinAndActor(string reportType, int[] recipientIds, string eventType)
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        var reportedAt = new DateTimeOffset(2026, 10, 11, 14, 0, 0, TimeSpan.Zero);
        mocks.ClockUtility.Setup(c => c.UtcNow).Returns(reportedAt);
        GrantAccess(mocks, RoleNames.Foreman);
        ProvideOptions(mocks, ReportOptions());
        StoreReportPhotos(mocks);
        var raised = AcceptRaise(mocks, new ReportSavedRecord(20, "FN-000020", [21, 22]));
        var request = Request();
        request.ReportType = reportType;

        // Act
        var result = await CreateMockManager(mocks).RaiseAsync(UserId, ProjectId, request, CancellationToken.None);

        // Assert
        var summary = ResultAssert.Succeeded(result);
        Assert.Equal(("FN-000020", "Steel missing"), (summary.Number, summary.Title));
        Assert.Equal(["Welding", "Coatings"], summary.BlockedCrewNames);
        Assert.Null(summary.ExpectedReadyAt);
        var write = Assert.Single(raised).Write;
        Assert.Equal((UserId, 2, 2), (write.ActorUserId, write.ReporterCrewId, write.Impacts.Count));
        Assert.Equal((0.25m, 0.75m), (write.PinX, write.PinY));
        Assert.Equal(DelayLocationSources.ManualPin, write.LocationSource);
        Assert.Equal(reportedAt, write.CreatedAt);
        Assert.Equal(reportType, write.ReportType);
        Assert.Equal(eventType, write.EventType);
        Assert.Equal(recipientIds, write.RecipientIds);
        var photo = Assert.Single(write.Photos);
        Assert.Equal((41m, -96m, "evidence.jpg", "image/jpeg"), (photo.Latitude, photo.Longitude, photo.FileName, photo.ContentType));
        await using var original = photo.OpenOriginal();
        await using var thumbnail = photo.OpenThumbnail();
        Assert.Equal((3, 3), (original.Length, thumbnail.Length));
    }

    [Fact]
    public async Task RaiseAsync_ValidReport_StoresThePhotosBeforeTheWriteRecordsThem()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman);
        ProvideOptions(mocks, ReportOptions());
        StoreReportPhotos(mocks);
        var raised = AcceptRaise(mocks, new ReportSavedRecord(20, "FN-000020", [21, 22]));

        // Act
        var result = await CreateMockManager(mocks).RaiseAsync(UserId, ProjectId, Request(), CancellationToken.None);

        // Assert
        ResultAssert.Succeeded(result);
        mocks.DelayPhotoAccessor.Verify(a => a.StoreReportPhotosAsync(ProjectId, 7, It.IsAny<DateTimeOffset>(),
            It.Is<IReadOnlyList<ReportPhotoUploadRecord>>(p => p.Single().FileName == "evidence.jpg"), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("stored/evidence.jpg", Assert.Single(Assert.Single(raised).Photos).OriginalUrl);
        mocks.DelayPhotoAccessor.Verify(a => a.RemoveAsync(It.IsAny<StoredPhotoSet>()), Times.Never);
    }

    [Fact]
    public async Task RaiseAsync_BundledMapWithoutSiteMapRecord_PreservesPin()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman);
        ProvideOptions(mocks, ReportOptions() with { SiteMap = null });
        StoreReportPhotos(mocks);
        var raised = AcceptRaise(mocks, new ReportSavedRecord(20, "FN-000020", [21, 22]));
        var request = Request();
        request.SiteMapId = null;
        request.Photos[0].Latitude = null;
        request.Photos[0].Longitude = null;

        // Act
        var result = await CreateMockManager(mocks).RaiseAsync(UserId, ProjectId, request, CancellationToken.None);

        // Assert
        var summary = ResultAssert.Succeeded(result);
        var write = Assert.Single(raised).Write;
        Assert.Null(write.SiteMapId);
        Assert.Equal((0.25m, 0.75m), (write.PinX, write.PinY));
        Assert.Equal(DelayLocationSources.ManualPin, write.LocationSource);
        Assert.Null(summary.SiteMapId);
        Assert.Equal(new DelayPinResponse(0.25m, 0.75m), summary.Pin);
    }

    [Theory]
    [InlineData("reason")]
    [InlineData("photos")]
    [InlineData("segment")]
    [InlineData("work-item")]
    [InlineData("impacts")]
    public async Task RaiseAsync_InvalidChoice_ReturnsBadRequestWithoutWriting(string field)
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman);
        ProvideOptions(mocks, ReportOptions());
        var request = Request();
        switch (field)
        {
            case "reason": request.DelayTagId = 99; break;
            case "photos": request.Photos.Clear(); break;
            case "segment": request.SegmentId = 99; break;
            case "work-item": request.BlockingWorkItemId = 8; break;
            case "impacts": request.Impacts.Clear(); break;
        }

        // Act
        var result = await CreateMockManager(mocks).RaiseAsync(UserId, ProjectId, request, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.InvalidRequest, result);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task RaiseAsync_Superintendent_RefusesBeforeFormReads()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent);

        // Act
        var result = await CreateMockManager(mocks).RaiseAsync(UserId, ProjectId, Request(), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        VerifyNoFormRead(mocks);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task RaiseAsync_NoProjectAccess_RefusesBeforeFormReads()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectAccessRecord?)null);

        // Act
        var act = () => CreateMockManager(mocks).RaiseAsync(UserId, ProjectId, Request(), CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        VerifyNoFormRead(mocks);
        AssertNothingWritten(mocks);
    }

    [Fact]
    public async Task RaiseAsync_CrewNotInvolved_RefusesWithoutWriting()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman);
        ProvideOptions(mocks, ReportOptions());
        var request = Request();
        request.Impacts = [new() { BlockedCrewId = 3 }];

        // Act
        var result = await CreateMockManager(mocks).RaiseAsync(UserId, ProjectId, request, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        AssertNothingWritten(mocks);
    }

    [Theory]
    [MemberData(nameof(AccessorFailures))]
    public async Task RaiseAsync_AccessorFails_RemovesTheStoredPhotosAndPropagatesTheFailure(Exception failure)
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman);
        ProvideOptions(mocks, ReportOptions());
        StoreReportPhotos(mocks);
        mocks.DelayAccessor
            .Setup(a => a.RaiseDelayAsync(It.IsAny<RaiseDelayWrite>(), It.IsAny<IReadOnlyList<ReportPhotoRecord>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        var manager = CreateMockManager(mocks);

        // Act
        var raise = () => manager.RaiseAsync(UserId, ProjectId, Request(), CancellationToken.None);

        // Assert
        var thrown = await Assert.ThrowsAnyAsync<Exception>(raise);
        Assert.Same(failure, thrown);
        mocks.DelayPhotoAccessor.Verify(a => a.RemoveAsync(It.Is<StoredPhotoSet>(s => s.StorageKeys.Single() == "stored/evidence.jpg")), Times.Once);
    }

    public static TheoryData<Exception> AccessorFailures()
    {
        return
        [
            new InvalidOperationException("Save failed"),
            new ArgumentException("Invalid storage configuration"),
            new OperationCanceledException(),
        ];
    }

    [Fact]
    public async Task GetReportOptionsAsync_Foreman_LoadsEveryOptionUpfront()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman);
        ProvideOptions(mocks, ReportOptions());

        // Act
        var result = await CreateMockManager(mocks).GetReportOptionsAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var options = ResultAssert.Succeeded(result);
        Assert.Equal((3, 2, 1, 1, 1), (options.Crews.Count, options.WorkItems.Count, options.Tags.Count, options.Segments.Count, options.People.Count));
        Assert.Equal(1, options.SiteMap!.Id);
    }

    [Fact]
    public async Task TranscribeReportAsync_SpokenCrewAndReason_SuggestsFieldsFromOnlyCrewsAndTags()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Foreman);
        ProvideOptions(mocks, ReportOptions());
        mocks.SpeechAccessor.Setup(a => a.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("The responsible crew is Steel. Materials have not arrived.");
        var request = new DelaySpeechRequest { Audio = Photo("recording.wav") };

        // Act
        var result = await CreateMockManager(mocks).TranscribeReportAsync(UserId, ProjectId, request, CancellationToken.None);

        // Assert
        var fields = ResultAssert.Succeeded(result);
        Assert.Equal((1, 1), (fields.OwnerCrewId, fields.DelayTagId));
        mocks.WorkItemAccessor.Verify(a => a.GetOpenWorkItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        mocks.ProjectAccessor.Verify(a => a.GetMembersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TranscribeReportAsync_Superintendent_RefusesBeforeCallingTheSpeechService()
    {
        // Arrange
        var mocks = new DelayManagerMocks();
        GrantAccess(mocks, RoleNames.Superintendent);
        var request = new DelaySpeechRequest { Audio = Photo("recording.wav") };

        // Act
        var result = await CreateMockManager(mocks).TranscribeReportAsync(UserId, ProjectId, request, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        mocks.SpeechAccessor.VerifyNoOtherCalls();
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

    private static void GrantAccess(DelayManagerMocks mocks, string roleName)
    {
        mocks.ProjectAccessor.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Project", roleName, []));
    }

    /// <summary>Each part of the report form comes from the Accessor that owns it.</summary>
    private static void ProvideOptions(DelayManagerMocks mocks, DelayReportOptionsRecord options)
    {
        mocks.ProjectAccessor.Setup(a => a.GetCrewsAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(options.Crews);
        mocks.ProjectAccessor.Setup(a => a.GetSegmentsAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(options.Segments);
        mocks.ProjectAccessor.Setup(a => a.GetActiveSiteMapAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(options.SiteMap);
        mocks.ProjectAccessor.Setup(a => a.GetMembersAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(options.People);
        mocks.WorkItemAccessor.Setup(a => a.GetOpenWorkItemsAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(options.WorkItems);
        mocks.DelayAccessor.Setup(a => a.GetDelayTagsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(options.Tags);
    }

    /// <summary>The photo Accessor stores every upload under its file name.</summary>
    private static void StoreReportPhotos(DelayManagerMocks mocks)
    {
        mocks.DelayPhotoAccessor
            .Setup(a => a.StoreReportPhotosAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<IReadOnlyList<ReportPhotoUploadRecord>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, int? _, DateTimeOffset _, IReadOnlyList<ReportPhotoUploadRecord> uploads, CancellationToken _) => new StoredPhotoSet(
                uploads.Select(u => new ReportPhotoRecord($"stored/{u.FileName}", $"stored/{u.FileName}.thumbnail", u.FileName, u.Latitude, u.Longitude)).ToList(),
                uploads.Select(u => $"stored/{u.FileName}").ToList()));
    }

    /// <summary>The delay Accessor commits the report as <paramref name="saved"/>; each write and the photos it recorded are captured.</summary>
    private static List<(RaiseDelayWrite Write, IReadOnlyList<ReportPhotoRecord> Photos)> AcceptRaise(DelayManagerMocks mocks, ReportSavedRecord saved)
    {
        var raised = new List<(RaiseDelayWrite, IReadOnlyList<ReportPhotoRecord>)>();
        mocks.DelayAccessor
            .Setup(a => a.RaiseDelayAsync(It.IsAny<RaiseDelayWrite>(), It.IsAny<IReadOnlyList<ReportPhotoRecord>>(), It.IsAny<CancellationToken>()))
            .Callback<RaiseDelayWrite, IReadOnlyList<ReportPhotoRecord>, CancellationToken>((write, photos, _) => raised.Add((write, photos)))
            .ReturnsAsync(saved);
        return raised;
    }

    private static void VerifyNoFormRead(DelayManagerMocks mocks)
    {
        mocks.ProjectAccessor.Verify(a => a.GetCrewsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        mocks.WorkItemAccessor.Verify(a => a.GetOpenWorkItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void AssertNothingWritten(DelayManagerMocks mocks)
    {
        mocks.DelayAccessor.Verify(
            a => a.RaiseDelayAsync(It.IsAny<RaiseDelayWrite>(), It.IsAny<IReadOnlyList<ReportPhotoRecord>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        mocks.DelayPhotoAccessor.Verify(a => a.StoreReportPhotosAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<IReadOnlyList<ReportPhotoUploadRecord>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static DelayReportOptionsRecord ReportOptions()
    {
        return new DelayReportOptionsRecord(
            [new(1, "Steel", 4, 10), new(2, "Welding", 3, 11), new(3, "Coatings", 5, 12)],
            [new(7, "Fit steel", [1]), new(8, "Weld frame", [2])],
            [new(1, "Bay 4")], [new(1, "MissingMaterial", "Missing material")],
            new(1, "Site", "/sitemap.png"), [new(10, "Superintendent", 1, "Steel", RoleNames.Superintendent)]);
    }

    private static RaiseDelayRequest Request()
    {
        return new RaiseDelayRequest
        {
            Title = "Steel missing",
            Description = "Missing steel stops welding",
            OwnerCrewId = 1,
            DelayTagId = 1,
            Impacts = [new() { BlockedCrewId = 2, BlockedWorkItemId = 8 }, new() { BlockedCrewId = 3 }],
            SegmentId = 1,
            BlockingWorkItemId = 7,
            SiteMapId = 1,
            PinX = 0.25m,
            PinY = 0.75m,
            ReportType = DelayReportTypes.AfterArrival,
            Photos = [new() { File = Photo("evidence.jpg"), Thumbnail = Photo("thumbnail.jpg"), Latitude = 41, Longitude = -96 }],
        };
    }

    private static FormFile Photo(string name)
    {
        return new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "photo", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg",
        };
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
