using Accessors;
using Accessors.Delays;
using Accessors.Projects;
using Common.Delays;
using Common.Errors;
using Common.Projects;
using Common.Roles;
using DataContract.Delays;
using Managers.Delays;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Managers;

public class DelayReportManagerTests
{
    private readonly Mock<IProjectAccessAccessor> _access = new();
    private readonly Mock<IDelayAccessor> _delays = new();
    private static readonly DelayReportOptionsRecord Options = new(
        [new(1, "Steel", 4, 10), new(2, "Welding", 3, 11), new(3, "Coatings", 5, 12)],
        [new(7, "Fit steel", [1]), new(8, "Weld frame", [2])],
        [new(1, "Bay 4")], [new(1, "MissingMaterial", "Missing material")],
        new(1, "Site", "/sitemap.png"), [new(10, "Superintendent", 1, "Steel", RoleNames.Superintendent)]);

    [Theory]
    [InlineData(DelayReportTypes.AfterArrival, new int[] { 10 })]
    [InlineData(DelayReportTypes.AdvanceNotice, new int[] { 10, 11, 12 })]
    public async Task CreateReportAsync_ValidReport_PreservesAllImpactsPhotosPinAndActor(string reportType, int[] recipientIds)
    {
        using var services = Services();
        DelayReportRecord? written = null;
        _delays.Setup(a => a.CreateReportAsync(It.IsAny<DelayReportRecord>(), It.IsAny<CancellationToken>()))
            .Callback<DelayReportRecord, CancellationToken>((r, _) => written = r)
            .ReturnsAsync(new ReportSavedRecord(20, "FN-000020", [21, 22]));
        var request = Request();
        request.ReportType = reportType;
        var result = await Manager(services).CreateReportAsync(3, 1, request, CancellationToken.None);

        Assert.Equal("Steel missing", result.Title);
        Assert.Equal(["Welding", "Coatings"], result.BlockedCrewNames);
        Assert.NotNull(written);
        Assert.Equal(3, written.ActorUserId);
        Assert.Equal(2, written.ReporterCrewId);
        Assert.Equal(2, written.Impacts.Count);
        Assert.Equal((0.25m, 0.75m), (written.PinX, written.PinY));
        Assert.Equal("ManualPin", written.LocationSource);
        Assert.Equal(TimeSpan.Zero, written.CreatedAt.Offset);
        Assert.Equal(reportType, written.ReportType);
        Assert.Null(result.ExpectedReadyAt);
        var photo = Assert.Single(written.Photos);
        Assert.Equal((41m, -96m), (photo.Latitude, photo.Longitude));
        Assert.Equal("evidence.jpg", photo.FileName);
        Assert.Equal("image/jpeg", photo.ContentType);
        await using var original = photo.OpenOriginal();
        await using var thumbnail = photo.OpenThumbnail();
        Assert.Equal(3, original.Length);
        Assert.Equal(3, thumbnail.Length);
        Assert.Equal(recipientIds, written.RecipientIds);
    }

    [Theory]
    [InlineData(DelayReportTypes.AfterArrival)]
    [InlineData(DelayReportTypes.AdvanceNotice)]
    public async Task CreateReportAsync_ReporterSuppliesCompletionTime_PreventsWrite(string reportType)
    {
        using var services = Services();
        var request = Request();
        request.ReportType = reportType;
        request.ExpectedReadyAt = new DateTimeOffset(2026, 10, 11, 14, 0, 0, TimeSpan.Zero);

        var error = await Assert.ThrowsAsync<DelayReportValidationException>(() =>
            Manager(services).CreateReportAsync(3, 1, request, CancellationToken.None));

        Assert.Contains("responsible Superintendent", error.Message);
        _delays.Verify(a => a.CreateReportAsync(It.IsAny<DelayReportRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateReportAsync_BundledMapWithoutSiteMapRecord_PreservesPin()
    {
        using var services = Services();
        _delays.Setup(a => a.GetReportOptionsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Options with { SiteMap = null });
        DelayReportRecord? written = null;
        _delays.Setup(a => a.CreateReportAsync(It.IsAny<DelayReportRecord>(), It.IsAny<CancellationToken>()))
            .Callback<DelayReportRecord, CancellationToken>((r, _) => written = r)
            .ReturnsAsync(new ReportSavedRecord(20, "FN-000020", [21, 22]));
        var request = Request();
        request.SiteMapId = null;
        request.Photos[0].Latitude = null;
        request.Photos[0].Longitude = null;

        var result = await Manager(services).CreateReportAsync(3, 1, request, CancellationToken.None);

        Assert.NotNull(written);
        Assert.Null(written.SiteMapId);
        Assert.Equal((0.25m, 0.75m), (written.PinX, written.PinY));
        Assert.Equal("ManualPin", written.LocationSource);
        Assert.Null(result.SiteMapId);
        Assert.NotNull(result.Pin);
        Assert.Equal((0.25m, 0.75m), (result.Pin.X, result.Pin.Y));
    }

    [Theory]
    [InlineData("reason")]
    [InlineData("photos")]
    [InlineData("segment")]
    [InlineData("work-item")]
    public async Task CreateReportAsync_InvalidRequiredInput_PreventsUploadsAndWrite(string field)
    {
        using var services = Services();
        var request = Request();
        switch (field)
        {
            case "reason": request.DelayTagId = 99; break;
            case "photos": request.Photos.Clear(); break;
            case "segment": request.SegmentId = 99; break;
            case "work-item": request.BlockingWorkItemId = 8; break;
        }
        await Assert.ThrowsAsync<DelayReportValidationException>(() =>
            Manager(services).CreateReportAsync(3, 1, request, CancellationToken.None));
        _delays.Verify(a => a.CreateReportAsync(It.IsAny<DelayReportRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateReportAsync_Superintendent_RejectsBeforeFormReads()
    {
        using var services = Services();
        _access.Setup(a => a.GetProjectAccessAsync(3, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(1, "Project", RoleNames.Superintendent, []));
        await Assert.ThrowsAsync<DelayReportForbiddenException>(() =>
            Manager(services).CreateReportAsync(3, 1, Request(), CancellationToken.None));
        _delays.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateReportAsync_NoProjectAccess_RejectsBeforeFormReads()
    {
        using var services = Services();
        _access.Setup(a => a.GetProjectAccessAsync(3, 1, It.IsAny<CancellationToken>())).ReturnsAsync((ProjectAccessRecord?)null);
        await Assert.ThrowsAsync<ProjectAccessDeniedException>(() =>
            Manager(services).CreateReportAsync(3, 1, Request(), CancellationToken.None));
        _delays.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateReportAsync_CrewNotInvolved_PreventsWrite()
    {
        using var services = Services();
        var request = Request();
        request.Impacts = [new() { BlockedCrewId = 3 }];
        await Assert.ThrowsAsync<DelayReportForbiddenException>(() =>
            Manager(services).CreateReportAsync(3, 1, request, CancellationToken.None));
        _delays.Verify(a => a.CreateReportAsync(It.IsAny<DelayReportRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateReportAsync_AccessorFails_PropagatesFailure()
    {
        using var services = Services();
        _delays.Setup(a => a.CreateReportAsync(It.IsAny<DelayReportRecord>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Save failed"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Manager(services).CreateReportAsync(3, 1, Request(), CancellationToken.None));
    }

    [Fact]
    public async Task GetReportOptionsAsync_LoadsEveryOptionUpfront()
    {
        using var services = Services();
        var result = await Manager(services).GetReportOptionsAsync(3, 1, CancellationToken.None);
        Assert.Equal(3, result.Crews.Count);
        Assert.Equal(2, result.WorkItems.Count);
        Assert.Single(result.Tags);
        Assert.Single(result.Segments);
        Assert.Single(result.People);
        Assert.Equal(1, result.SiteMap!.Id);
    }

    private ServiceProvider Services()
    {
        _access.Setup(a => a.GetProjectAccessAsync(3, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectAccessRecord(1, "Project", RoleNames.Foreman, []));
        _delays.Setup(a => a.GetReportOptionsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Options);
        var services = new ServiceCollection();
        services.AddSingleton(_access.Object);
        services.AddSingleton(_delays.Object);
        return services.BuildServiceProvider();
    }

    private DelayReportManager Manager(ServiceProvider services) =>
        new(new AccessorFactory(services));

    private static DelayReportRequest Request() => new()
    {
        Title = "Steel missing", Description = "Missing steel stops welding", OwnerCrewId = 1, DelayTagId = 1,
        Impacts = [new() { BlockedCrewId = 2, BlockedWorkItemId = 8 }, new() { BlockedCrewId = 3 }],
        SegmentId = 1, BlockingWorkItemId = 7, SiteMapId = 1, PinX = 0.25m, PinY = 0.75m,
        ReportType = DelayReportTypes.AfterArrival,
        Photos = [new() { File = Photo("evidence.jpg"), Thumbnail = Photo("thumbnail.jpg"), Latitude = 41, Longitude = -96 }],
    };

    private static FormFile Photo(string name) => new(new MemoryStream([1, 2, 3]), 0, 3, "photo", name)
    {
        Headers = new HeaderDictionary(), ContentType = "image/jpeg",
    };
}
