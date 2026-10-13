using Accessors.Delays;
using Accessors.Projects;
using Accessors.Speech;
using Common.Delays;
using Common.Errors;
using Common.Roles;
using DataContract.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Home;
using Managers.Delays;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Tests.Managers;

/// <summary>Raising a delay, the report form behind it, and the spoken shortcut that fills that form.</summary>
public class DelayManagerRaiseTests
{
    private const int UserId = 3;
    private const int ProjectId = 1;

    private static readonly DelayReportOptionsRecord Options = new(
        [new(1, "Steel", 4, 10), new(2, "Welding", 3, 11), new(3, "Coatings", 5, 12)],
        [new(7, "Fit steel", [1]), new(8, "Weld frame", [2])],
        [new(1, "Bay 4")], [new(1, "MissingMaterial", "Missing material")],
        new(1, "Site", "/sitemap.png"), [new(10, "Superintendent", 1, "Steel", RoleNames.Superintendent)]);

    private readonly Mock<IProjectAccessAccessor> _access = new();
    private readonly Mock<IDelayAccessor> _delays = new();
    private readonly Mock<ISpeechAccessor> _speech = new();
    private RaiseDelayWrite? _written;

    public DelayManagerRaiseTests()
    {
        GrantAccess(RoleNames.Foreman);
        _delays.Setup(a => a.GetReportOptionsAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(Options);
        _delays.Setup(a => a.CreateDelayAsync(It.IsAny<RaiseDelayWrite>(), It.IsAny<CancellationToken>()))
            .Callback<RaiseDelayWrite, CancellationToken>((write, _) => _written = write)
            .ReturnsAsync(new ReportSavedRecord(20, "FN-000020", [21, 22]));
    }

    [Theory]
    [InlineData(DelayReportTypes.AfterArrival, new int[] { 10 })]
    [InlineData(DelayReportTypes.AdvanceNotice, new int[] { 10, 11, 12 })]
    public async Task RaiseAsync_ValidReport_PreservesAllImpactsPhotosPinAndActor(string reportType, int[] recipientIds)
    {
        // Arrange
        var request = Request();
        request.ReportType = reportType;

        // Act
        var result = await CreateManager().RaiseAsync(UserId, ProjectId, request, CancellationToken.None);

        // Assert
        var summary = ResultAssert.Succeeded(result);
        Assert.Equal("Steel missing", summary.Title);
        Assert.Equal(["Welding", "Coatings"], summary.BlockedCrewNames);
        Assert.Null(summary.ExpectedReadyAt);
        var write = Assert.IsType<RaiseDelayWrite>(_written);
        Assert.Equal((UserId, 2, 2), (write.ActorUserId, write.ReporterCrewId, write.Impacts.Count));
        Assert.Equal((0.25m, 0.75m), (write.PinX, write.PinY));
        Assert.Equal("ManualPin", write.LocationSource);
        Assert.Equal(TimeSpan.Zero, write.CreatedAt.Offset);
        Assert.Equal(reportType, write.ReportType);
        Assert.Equal(recipientIds, write.RecipientIds);
        var photo = Assert.Single(write.Photos);
        Assert.Equal((41m, -96m, "evidence.jpg", "image/jpeg"), (photo.Latitude, photo.Longitude, photo.FileName, photo.ContentType));
        await using var original = photo.OpenOriginal();
        await using var thumbnail = photo.OpenThumbnail();
        Assert.Equal((3, 3), (original.Length, thumbnail.Length));
    }

    [Fact]
    public async Task RaiseAsync_BundledMapWithoutSiteMapRecord_PreservesPin()
    {
        // Arrange
        _delays.Setup(a => a.GetReportOptionsAsync(ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync(Options with { SiteMap = null });
        var request = Request();
        request.SiteMapId = null;
        request.Photos[0].Latitude = null;
        request.Photos[0].Longitude = null;

        // Act
        var result = await CreateManager().RaiseAsync(UserId, ProjectId, request, CancellationToken.None);

        // Assert
        var summary = ResultAssert.Succeeded(result);
        var write = Assert.IsType<RaiseDelayWrite>(_written);
        Assert.Null(write.SiteMapId);
        Assert.Equal((0.25m, 0.75m), (write.PinX, write.PinY));
        Assert.Equal("ManualPin", write.LocationSource);
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
        var result = await CreateManager().RaiseAsync(UserId, ProjectId, request, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.InvalidRequest, result);
        AssertNothingWritten();
    }

    [Fact]
    public async Task RaiseAsync_Superintendent_RefusesBeforeFormReads()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent);

        // Act
        var result = await CreateManager().RaiseAsync(UserId, ProjectId, Request(), CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        _delays.Verify(a => a.GetReportOptionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        AssertNothingWritten();
    }

    [Fact]
    public async Task RaiseAsync_NoProjectAccess_RefusesBeforeFormReads()
    {
        // Arrange
        _access.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>())).ReturnsAsync((ProjectAccessRecord?)null);

        // Act
        var act = () => CreateManager().RaiseAsync(UserId, ProjectId, Request(), CancellationToken.None);

        // Assert
        await AuthorizationAssert.DeniedAsync(ErrorCodes.Forbidden, act);
        _delays.Verify(a => a.GetReportOptionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        AssertNothingWritten();
    }

    [Fact]
    public async Task RaiseAsync_CrewNotInvolved_RefusesWithoutWriting()
    {
        // Arrange
        var request = Request();
        request.Impacts = [new() { BlockedCrewId = 3 }];

        // Act
        var result = await CreateManager().RaiseAsync(UserId, ProjectId, request, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        AssertNothingWritten();
    }

    [Theory]
    [MemberData(nameof(AccessorFailures))]
    public async Task RaiseAsync_AccessorFails_PropagatesTheFailure(Exception failure)
    {
        // Arrange
        _delays.Setup(a => a.CreateDelayAsync(It.IsAny<RaiseDelayWrite>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var manager = CreateManager();

        // Act
        var raise = () => manager.RaiseAsync(UserId, ProjectId, Request(), CancellationToken.None);

        // Assert
        var thrown = await Assert.ThrowsAnyAsync<Exception>(raise);
        Assert.Same(failure, thrown);
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
        // Act
        var result = await CreateManager().GetReportOptionsAsync(UserId, ProjectId, CancellationToken.None);

        // Assert
        var options = ResultAssert.Succeeded(result);
        Assert.Equal((3, 2, 1, 1, 1), (options.Crews.Count, options.WorkItems.Count, options.Tags.Count, options.Segments.Count, options.People.Count));
        Assert.Equal(1, options.SiteMap!.Id);
    }

    [Fact]
    public async Task TranscribeReportAsync_SpokenCrewAndReason_SuggestsFieldsFromOnlyCrewsAndTags()
    {
        // Arrange
        _delays.Setup(a => a.GetReportChoicesAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportChoicesRecord(Options.Crews, Options.Tags));
        _speech.Setup(a => a.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("The responsible crew is Steel. Materials have not arrived.");
        var request = new DelaySpeechRequest { Audio = Photo("recording.wav") };

        // Act
        var result = await CreateManager().TranscribeReportAsync(UserId, ProjectId, request, CancellationToken.None);

        // Assert
        var fields = ResultAssert.Succeeded(result);
        Assert.Equal((1, 1), (fields.OwnerCrewId, fields.DelayTagId));
        _delays.Verify(a => a.GetReportOptionsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TranscribeReportAsync_Superintendent_RefusesBeforeCallingTheSpeechService()
    {
        // Arrange
        GrantAccess(RoleNames.Superintendent);
        var request = new DelaySpeechRequest { Audio = Photo("recording.wav") };

        // Act
        var result = await CreateManager().TranscribeReportAsync(UserId, ProjectId, request, CancellationToken.None);

        // Assert
        ResultAssert.Failed(ErrorCodes.Forbidden, result);
        _speech.VerifyNoOtherCalls();
    }

    #region Private Methods

    private void GrantAccess(string roleName)
    {
        _access.Setup(a => a.GetProjectAccessAsync(UserId, ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(ProjectId, "Project", roleName, []));
    }

    private void AssertNothingWritten()
    {
        Assert.Null(_written);
        _delays.Verify(a => a.CreateDelayAsync(It.IsAny<RaiseDelayWrite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private DelayManager CreateManager()
    {
        return new ManagerTestHost().With(_access).With(_delays).With(_speech).Create<DelayManager>();
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

    #endregion
}
