using Common.Delays;
using DataContract.Domain.Delays;
using DataContract.Domain.Projects;
using DataContract.Home;
using DataContract.Mapping.DomainToClientMapping;

namespace Tests.Mapping.DomainToClientMapping;

public class DelayDomainToClientMappingTests
{
    private static readonly ProjectAccessRecord Access = new(1, "Omaha Fabrication", "Foreman", []);
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 5, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToResponse_ReportOptions_MapsEveryOptionWithTheProject()
    {
        // Arrange
        var options = new DelayReportOptionsRecord(
            [new ReportCrewRecord(1, "Steel", 4, 10)],
            [new ReportWorkItemRecord(7, "Fit steel", [1])],
            [new ReportSegmentRecord(2, "Bay 4")],
            [new ReportTagRecord(3, "MissingMaterial", "Missing material")],
            new ReportMapRecord(5, "Site", "/sitemap.png"),
            [new ReportPersonRecord(10, "Dana", 1, "Steel", "Superintendent")]);

        // Act
        var response = options.ToResponse(Access);

        // Assert
        Assert.Equal((1, "Omaha Fabrication"), (response.ProjectId, response.ProjectName));
        Assert.Equal("Steel", Assert.Single(response.Crews).Name);
        Assert.Equal([1], Assert.Single(response.WorkItems).CrewIds);
        Assert.Equal("Bay 4", Assert.Single(response.Segments).Name);
        Assert.Equal("MissingMaterial", Assert.Single(response.Tags).Code);
        Assert.Equal((5, "/sitemap.png"), (response.SiteMap!.Id, response.SiteMap.Uri));
        Assert.Equal("Dana", Assert.Single(response.People).DisplayName);
    }

    [Fact]
    public void ToResponse_ReportOptionsWithNullCollections_MapsToEmptyLists()
    {
        // Arrange
        var options = new DelayReportOptionsRecord(null!, null!, null!, null!, null, null!);

        // Act
        var response = options.ToResponse(Access);

        // Assert
        Assert.Empty(response.Crews);
        Assert.Empty(response.WorkItems);
        Assert.Empty(response.Segments);
        Assert.Empty(response.Tags);
        Assert.Null(response.SiteMap);
        Assert.Empty(response.People);
    }

    [Fact]
    public void ToResponse_ReportOptionsWithEmptyCollections_MapsToEmptyLists()
    {
        // Arrange
        var options = new DelayReportOptionsRecord([], [], [], [], null, []);

        // Act
        var response = options.ToResponse(Access);

        // Assert
        Assert.Empty(response.Crews);
        Assert.Empty(response.WorkItems);
        Assert.Empty(response.Segments);
        Assert.Empty(response.Tags);
        Assert.Empty(response.People);
    }

    [Fact]
    public void ToResponse_WorkItemWithNullCrews_MapsToAnEmptyList()
    {
        // Arrange
        var workItem = new ReportWorkItemRecord(7, "Fit steel", null!);

        // Act
        var response = workItem.ToResponse();

        // Assert
        Assert.Empty(response.CrewIds);
    }

    [Fact]
    public void ToSummaryResponse_ActiveDelay_NamesEachAffectedCrewOnce()
    {
        // Arrange
        var delay = ActiveDelay();
        List<DelayImpactResponse> impacts = [new(10, "Welding", 8), new(11, "Welding", 9), new(12, "Coatings", null)];

        // Act
        var summary = delay.ToSummaryResponse(impacts, Access, isHighPriority: true);

        // Assert
        Assert.Equal(["Welding", "Coatings"], summary.BlockedCrewNames);
        Assert.Equal(3, summary.Impacts.Count);
        Assert.Equal(("FN-007", 1, "Omaha Fabrication", true), (summary.Number, summary.ProjectId, summary.ProjectName, summary.IsHighPriority));
        Assert.Equal(new DelayPinResponse(0.25m, 0.75m), summary.Pin);
    }

    [Fact]
    public void ToSummaryResponse_ActiveDelayWithNullImpacts_MapsToEmptyLists()
    {
        // Arrange
        var delay = ActiveDelay();

        // Act
        var summary = delay.ToSummaryResponse(null, Access, isHighPriority: false);

        // Assert
        Assert.Empty(summary.BlockedCrewNames);
        Assert.Empty(summary.Impacts);
    }

    [Fact]
    public void ToSummaryResponse_ActiveDelayWithEmptyImpacts_MapsToEmptyLists()
    {
        // Arrange
        var delay = ActiveDelay();

        // Act
        var summary = delay.ToSummaryResponse([], Access, isHighPriority: false);

        // Assert
        Assert.Empty(summary.BlockedCrewNames);
        Assert.Empty(summary.Impacts);
    }

    [Fact]
    public void ToSummaryResponse_RaisedDelay_ResolvesNamesAndPairsSavedImpactIds()
    {
        // Arrange
        var write = RaisedDelay([new ReportImpactRecord(2, 8), new ReportImpactRecord(3, null)], locationName: null);
        var saved = new ReportSavedRecord(20, "FN-000020", [21, 22]);

        // Act
        var summary = write.ToSummaryResponse(saved, Access, ReportOptions());

        // Assert
        Assert.Equal((20, "FN-000020", DelayStatusCodes.Raised), (summary.Id, summary.Number, summary.StatusCode));
        Assert.Equal(("Steel", "MissingMaterial"), (summary.OwnerCrewName, summary.TagCode));
        Assert.Equal(["Welding", "Coatings"], summary.BlockedCrewNames);
        Assert.Equal([21, 22], summary.Impacts.Select(i => i.Id));
        Assert.Equal("Bay 4", summary.LocationName);
        Assert.False(summary.IsHighPriority);
    }

    [Fact]
    public void ToSummaryResponse_RaisedDelayWithNullImpacts_MapsToEmptyLists()
    {
        // Arrange
        var write = RaisedDelay(null!, locationName: "North bay");
        var saved = new ReportSavedRecord(20, "FN-000020", []);

        // Act
        var summary = write.ToSummaryResponse(saved, Access, ReportOptions());

        // Assert
        Assert.Empty(summary.BlockedCrewNames);
        Assert.Empty(summary.Impacts);
        Assert.Equal("North bay", summary.LocationName);
    }

    [Fact]
    public void ToSummaryResponse_RaisedDelayWithEmptyImpacts_MapsToEmptyLists()
    {
        // Arrange
        var write = RaisedDelay([], locationName: null);
        var saved = new ReportSavedRecord(20, "FN-000020", []);

        // Act
        var summary = write.ToSummaryResponse(saved, Access, ReportOptions());

        // Assert
        Assert.Empty(summary.BlockedCrewNames);
        Assert.Empty(summary.Impacts);
    }

    [Theory]
    [InlineData(DelayStatusCodes.Acknowledged, DelayStatusCodes.Acknowledged)]
    [InlineData(null, DelayStatusCodes.Raised)]
    public void ToResponse_WorkflowStep_ReportsWhereTheDelayNowStands(string? toStatusCode, string expectedStatusCode)
    {
        // Arrange
        var write = new DelayWorkflowWrite(7, 1, 2, CreatedAt, DelayEventTypes.Acknowledged, DelayStatusCodes.Raised, toStatusCode,
            DelayCycleChange.Touch(5), [40]);

        // Act
        var response = write.ToResponse(new DelayWorkflowResult(5, 4900, 2));

        // Assert
        Assert.Equal((7, expectedStatusCode, 5, 4900, 2), (response.DelayId, response.StatusCode, response.CycleId, response.ActivityEventId, response.NoticeCount));
        Assert.Equal(CreatedAt, response.OccurredAt);
    }

    [Fact]
    public void ToResponse_Photo_SignsBothUrls()
    {
        // Arrange
        var photo = new TrackingPhotoRecord(1, 7, "InitialReport", "original.jpg", "thumbnail.jpg", "photo.jpg", CreatedAt, 41, -96);

        // Act
        var response = photo.ToResponse(url => $"{url}?signed");

        // Assert
        Assert.Equal(("original.jpg?signed", "thumbnail.jpg?signed"), (response.OriginalUrl, response.ThumbnailUrl));
    }

    [Theory]
    [InlineData(0.5, null)]
    [InlineData(null, 0.5)]
    [InlineData(null, null)]
    public void ToPinResponse_MissingCoordinate_HasNoPin(double? x, double? y)
    {
        // Act
        var pin = DelayDomainToClientMapping.ToPinResponse((decimal?)x, (decimal?)y);

        // Assert
        Assert.Null(pin);
    }

    #region Private Test Helper

    private static ActiveDelayRecord ActiveDelay()
    {
        return new ActiveDelayRecord(7, "FN-007", "Steel blocker", "Missing steel", DelayStatusCodes.Raised, "MissingMaterial",
            "Missing material", 1, "Steel", DelayReportTypes.AfterArrival, "Bay 2", "Bay 2", 5, 0.25m, 0.75m, CreatedAt, 1, true, null);
    }

    private static RaiseDelayWrite RaisedDelay(IReadOnlyList<ReportImpactRecord> impacts, string? locationName)
    {
        return new RaiseDelayWrite(1, 3, 2, "Steel missing", "Missing steel stops welding", 1, 1, DelayReportTypes.AfterArrival, null,
            2, null, null, null, DelayLocationSources.ManualEntry, locationName, CreatedAt, impacts, [], [10], DelayEventTypes.Raised);
    }

    private static DelayReportOptionsRecord ReportOptions()
    {
        return new DelayReportOptionsRecord(
            [new(1, "Steel", 4, 10), new(2, "Welding", 3, 11), new(3, "Coatings", 5, 12)],
            [], [new ReportSegmentRecord(2, "Bay 4")], [new ReportTagRecord(1, "MissingMaterial", "Missing material")], null, []);
    }

    #endregion
}
