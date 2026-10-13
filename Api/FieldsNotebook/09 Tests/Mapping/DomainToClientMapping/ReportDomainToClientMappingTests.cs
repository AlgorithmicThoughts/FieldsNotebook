using DataContract.Domain.Reports;
using DataContract.Mapping.DomainToClientMapping;

namespace Tests.Mapping.DomainToClientMapping;

public class ReportDomainToClientMappingTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 5, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToResponse_Report_GivesEachDelayOnlyTheCrewsItBlocks()
    {
        // Arrange
        var report = new DelayReportSet(
            [Delay(7, "FN-007"), Delay(8, "FN-008")],
            [new DelayReportImpactRecord(7, "Erection"), new DelayReportImpactRecord(7, "Welding"), new DelayReportImpactRecord(8, "Coatings")]);

        // Act
        var rows = report.ToResponse();

        // Assert
        Assert.Equal(["FN-007", "FN-008"], rows.Select(r => r.DelayNumber));
        Assert.Equal(["Erection", "Welding"], rows[0].BlockedCrewNames);
        Assert.Equal(["Coatings"], rows[1].BlockedCrewNames);
        Assert.Equal((CreatedAt.AddDays(1), 2, 3), (rows[0].ResolvedAt, rows[0].CycleCount, rows[0].PhotoCount));
    }

    [Fact]
    public void ToResponse_DelayWithNullImpacts_HasNoBlockedCrews()
    {
        // Arrange
        var report = new DelayReportSet([Delay(7, "FN-007")], null!);

        // Act
        var rows = report.ToResponse();

        // Assert
        Assert.Empty(Assert.Single(rows).BlockedCrewNames);
    }

    [Fact]
    public void ToResponse_NullDelays_MapsToAnEmptyList()
    {
        // Arrange
        var report = new DelayReportSet(null!, null!);

        // Act
        var rows = report.ToResponse();

        // Assert
        Assert.Empty(rows);
    }

    [Fact]
    public void ToResponse_EmptyReport_MapsToAnEmptyList()
    {
        // Arrange
        var report = DelayReportSet.Empty;

        // Act
        var rows = report.ToResponse();

        // Assert
        Assert.Empty(rows);
    }

    #region Private Test Helper

    private static DelayReportRecord Delay(int id, string number)
    {
        return new DelayReportRecord(id, number, "Omaha Fabrication", "Steel blocker", "Resolved", "Missing material",
            "AfterArrival", "Fitting", "North bay", null, "Dana Foreman", CreatedAt, CreatedAt.AddDays(1), 2, 3);
    }

    #endregion
}
