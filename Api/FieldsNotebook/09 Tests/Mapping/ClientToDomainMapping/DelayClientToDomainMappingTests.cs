using DataContract.Delays;
using DataContract.Domain.Delays;
using DataContract.Mapping.ClientToDomainMapping;
using Microsoft.AspNetCore.Http;

namespace Tests.Mapping.ClientToDomainMapping;

public class DelayClientToDomainMappingTests
{
    [Fact]
    public void ToDomain_Acknowledgment_KeepsTheChoiceAndNote()
    {
        // Arrange
        var request = new AcknowledgeDelayRequest("WorkRemains", "Steel arrives at noon.", DateTimeOffset.UnixEpoch);

        // Act
        var change = request.ToDomain();

        // Assert
        Assert.Equal(new DelayAcknowledgmentChange("WorkRemains", "Steel arrives at noon."), change);
    }

    [Fact]
    public void ToDomain_Verification_AttachesTheRouteImpact()
    {
        // Arrange
        var request = new VerifyHandoverRequest("Bay is clear.");

        // Act
        var change = request.ToDomain(impactId: 10);

        // Assert
        Assert.Equal(new DelayVerificationChange(10, "Bay is clear."), change);
    }

    [Fact]
    public void ToDomain_Impacts_MapsEachCrewAndWorkItemInOrder()
    {
        // Arrange
        List<DelayReportImpact> impacts = [new() { BlockedCrewId = 2, BlockedWorkItemId = 8 }, new() { BlockedCrewId = 3 }];

        // Act
        var records = impacts.ToDomain();

        // Assert
        Assert.Equal([new ReportImpactRecord(2, 8), new ReportImpactRecord(3, null)], records);
    }

    [Fact]
    public void ToDomain_NullImpacts_MapsToAnEmptyList()
    {
        // Arrange
        List<DelayReportImpact>? impacts = null;

        // Act
        var records = impacts.ToDomain();

        // Assert
        Assert.Empty(records);
    }

    [Fact]
    public void ToDomain_EmptyImpacts_MapsToAnEmptyList()
    {
        // Arrange
        List<DelayReportImpact> impacts = [];

        // Act
        var records = impacts.ToDomain();

        // Assert
        Assert.Empty(records);
    }

    [Fact]
    public void ToDomain_Photos_KeepsGpsAndOnlyTheFileName()
    {
        // Arrange
        List<DelayReportPhoto> photos =
        [
            new() { File = Upload("uploads/evidence.jpg", "image/jpeg"), Thumbnail = Upload("thumbnail.jpg", "image/jpeg"), Latitude = 41, Longitude = -96 },
        ];

        // Act
        var records = photos.ToDomain();

        // Assert
        var photo = Assert.Single(records);
        Assert.Equal(("evidence.jpg", "image/jpeg", 41m, -96m), (photo.FileName, photo.ContentType, photo.Latitude, photo.Longitude));
        using var original = photo.OpenOriginal();
        Assert.Equal(3, original.Length);
    }

    [Fact]
    public void ToDomain_NullPhotos_MapsToAnEmptyList()
    {
        // Arrange
        List<DelayReportPhoto>? photos = null;

        // Act
        var records = photos.ToDomain();

        // Assert
        Assert.Empty(records);
    }

    [Fact]
    public void ToDomain_EmptyPhotos_MapsToAnEmptyList()
    {
        // Arrange
        List<DelayReportPhoto> photos = [];

        // Act
        var records = photos.ToDomain();

        // Assert
        Assert.Empty(records);
    }

    private static FormFile Upload(string name, string contentType)
    {
        return new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "photo", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}
