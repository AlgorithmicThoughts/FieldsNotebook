using System.ComponentModel.DataAnnotations;
using DataContract.Delays;
using DataContract.Me;
using DataContract.Reports;
using DataContract.Validation;
using Microsoft.AspNetCore.Http;

namespace Tests.Contracts;

/// <summary>Request rules checked at model binding, so a broken request is a 400 before any Manager runs.</summary>
public class RequestValidationTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void PositiveId_AcceptsOnlyIdsOfOneOrMore(int id, bool expected)
    {
        // Arrange
        var attribute = new PositiveIdAttribute();

        // Act
        var isValid = attribute.IsValid(id);

        // Assert
        Assert.Equal(expected, isValid);
    }

    [Fact]
    public void UserSettingsRequest_NothingChosen_IsRefused()
    {
        // Arrange
        var request = new UserSettingsRequest(null, null, null);

        // Act
        var errors = Validate(request);

        // Assert
        Assert.Equal("Choose a setting to change.", Assert.Single(errors).ErrorMessage);
    }

    [Fact]
    public void UserSettingsRequest_OneSettingChosen_IsAccepted()
    {
        // Arrange
        var request = new UserSettingsRequest(null, false, null);

        // Act
        var errors = Validate(request);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void DelayReportRequest_NonPositiveProject_IsRefused()
    {
        // Arrange
        var request = new DelayReportRequest([1, 0], null, null);

        // Act
        var errors = Validate(request);

        // Assert
        Assert.Equal("Project IDs must be greater than zero.", Assert.Single(errors).ErrorMessage);
    }

    [Fact]
    public void DelayReportRequest_StartAfterEnd_IsRefused()
    {
        // Arrange
        var request = new DelayReportRequest([1], new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 1));

        // Act
        var errors = Validate(request);

        // Assert
        Assert.Equal("The start date must be on or before the end date.", Assert.Single(errors).ErrorMessage);
    }

    [Fact]
    public void RaiseDelayRequest_OnePinCoordinate_IsRefused()
    {
        // Arrange
        var request = new RaiseDelayRequest { PinX = 0.5m, LocationName = "Bay 4" };

        // Act
        var errors = Validate(request);

        // Assert
        Assert.Equal("Both pin coordinates are required.", Assert.Single(errors).ErrorMessage);
    }

    [Fact]
    public void RaiseDelayRequest_NoPinNameOrPhotoGps_IsRefused()
    {
        // Arrange
        var request = new RaiseDelayRequest { Photos = [new DelayReportPhoto()] };

        // Act
        var errors = Validate(request);

        // Assert
        Assert.Equal("Place a site pin or enter a location name when photos have no GPS.", Assert.Single(errors).ErrorMessage);
    }

    [Fact]
    public void RaiseDelayRequest_PhotoGpsOnly_IsAccepted()
    {
        // Arrange
        var request = new RaiseDelayRequest { Photos = [new DelayReportPhoto { Latitude = 41, Longitude = -96 }] };

        // Act
        var errors = Validate(request);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void DelayReportPhoto_LatitudeWithoutLongitude_IsRefused()
    {
        // Arrange
        var photo = new DelayReportPhoto { Latitude = 41 };

        // Act
        var errors = Validate(photo);

        // Assert
        Assert.Equal("Photo GPS requires both latitude and longitude.", Assert.Single(errors).ErrorMessage);
    }

    [Fact]
    public void DelaySpeechRequest_EmptyRecording_IsRefused()
    {
        // Arrange
        var request = new DelaySpeechRequest { Audio = new FormFile(new MemoryStream(), 0, 0, "audio", "recording.wav") };

        // Act
        var errors = Validate(request);

        // Assert
        Assert.Equal("Audio is required.", Assert.Single(errors).ErrorMessage);
    }

    private static IReadOnlyList<ValidationResult> Validate(IValidatableObject request)
    {
        return request.Validate(new ValidationContext(request)).ToList();
    }
}
