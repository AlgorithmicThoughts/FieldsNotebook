using DataContract.Mapping.ClientToDomainMapping;
using DataContract.Reports;

namespace Tests.Mapping.ClientToDomainMapping;

public class ReportClientToDomainMappingTests
{
    [Fact]
    public void ToDomain_InclusiveDays_BecomeUtcBoundsWithDistinctProjects()
    {
        // Arrange
        var request = new DelayReportRequest([1, 3, 1], new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

        // Act
        var query = request.ToDomain();

        // Assert
        Assert.Equal([1, 3], query.ProjectIds);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), query.CreatedFrom);
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero), query.CreatedBefore);
    }

    [Fact]
    public void ToDomain_OpenDates_LeaveBothBoundsOpen()
    {
        // Arrange
        var request = new DelayReportRequest([1], null, null);

        // Act
        var query = request.ToDomain();

        // Assert
        Assert.Null(query.CreatedFrom);
        Assert.Null(query.CreatedBefore);
    }

    [Fact]
    public void ToDomain_NullProjects_MapsToAnEmptyScope()
    {
        // Arrange
        var request = new DelayReportRequest(null, null, null);

        // Act
        var query = request.ToDomain();

        // Assert
        Assert.Empty(query.ProjectIds);
    }

    [Fact]
    public void ToDomain_EmptyProjects_MapsToAnEmptyScope()
    {
        // Arrange
        var request = new DelayReportRequest([], null, null);

        // Act
        var query = request.ToDomain();

        // Assert
        Assert.Empty(query.ProjectIds);
    }
}
