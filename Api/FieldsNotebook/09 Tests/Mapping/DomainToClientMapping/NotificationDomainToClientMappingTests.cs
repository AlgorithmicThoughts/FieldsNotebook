using DataContract.Domain.Notifications;
using DataContract.Mapping.DomainToClientMapping;

namespace Tests.Mapping.DomainToClientMapping;

public class NotificationDomainToClientMappingTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToListResponse_Notices_KeepsOrderAndCountsTheUnread()
    {
        // Arrange
        List<NotificationRecord> notices = [Notice(12, readAt: null), Notice(11, readAt: CreatedAt.AddHours(1)), Notice(10, readAt: null)];

        // Act
        var list = notices.ToListResponse();

        // Assert
        Assert.Equal([12, 11, 10], list.Notices.Select(n => n.Id));
        Assert.Equal(2, list.UnreadCount);
    }

    [Fact]
    public void ToListResponse_NullNotices_MapsToAnEmptyListWithNoUnread()
    {
        // Arrange
        List<NotificationRecord>? notices = null;

        // Act
        var list = notices.ToListResponse();

        // Assert
        Assert.Empty(list.Notices);
        Assert.Equal(0, list.UnreadCount);
    }

    [Fact]
    public void ToListResponse_EmptyNotices_MapsToAnEmptyListWithNoUnread()
    {
        // Arrange
        List<NotificationRecord> notices = [];

        // Act
        var list = notices.ToListResponse();

        // Assert
        Assert.Empty(list.Notices);
        Assert.Equal(0, list.UnreadCount);
    }

    [Fact]
    public void ToReadResponse_OpenedNotice_CarriesTheNewUnreadTotal()
    {
        // Arrange
        var notice = Notice(12, readAt: CreatedAt.AddHours(1));

        // Act
        var read = notice.ToReadResponse(unreadCount: 4);

        // Assert
        Assert.Equal((12, CreatedAt.AddHours(1), 4), (read.Notice.Id, read.Notice.ReadAt, read.UnreadCount));
    }

    private static NotificationRecord Notice(int id, DateTimeOffset? readAt)
    {
        return new NotificationRecord(id, 1, "FN-1042", "Primer not cured", "delay.raised", CreatedAt, readAt);
    }
}
