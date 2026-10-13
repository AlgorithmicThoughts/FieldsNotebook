using DataContract.Domain.Activity;
using DataContract.Mapping.DomainToClientMapping;

namespace Tests.Mapping.DomainToClientMapping;

public class HomeDomainToClientMappingTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 10, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToResponse_Activity_MapsEveryEventInOrder()
    {
        // Arrange
        List<ActivityEventRecord> activity =
        [
            new(42, "delay.reopened", "FN-006", "Bay 3 weld defect", "Priya Shah", "Resolved", "Reopened", null, null, CreatedAt),
            new(41, "delay.handoverVerified", "FN-005", "Primer", "Dana", null, null, null, "Coatings", CreatedAt.AddHours(-1)),
        ];

        // Act
        var entries = activity.ToResponse();

        // Assert
        Assert.Equal([42, 41], entries.Select(e => e.Id));
        Assert.Equal(("delay.reopened", "Priya Shah", "Reopened"), (entries[0].EventType, entries[0].ActorName, entries[0].ToStatusCode));
        Assert.Equal("Coatings", entries[1].AffectedCrewName);
    }

    [Fact]
    public void ToResponse_NullActivity_MapsToAnEmptyList()
    {
        // Arrange
        List<ActivityEventRecord>? activity = null;

        // Act
        var entries = activity.ToResponse();

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public void ToResponse_EmptyActivity_MapsToAnEmptyList()
    {
        // Arrange
        List<ActivityEventRecord> activity = [];

        // Act
        var entries = activity.ToResponse();

        // Assert
        Assert.Empty(entries);
    }
}
