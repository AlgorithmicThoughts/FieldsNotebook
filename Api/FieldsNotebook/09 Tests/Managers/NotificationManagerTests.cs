using Accessors.Notifications;
using Accessors.Projects;
using Common.Errors;
using Common.Notifications;
using Common.Projects;
using Common.Roles;
using Managers.Notifications;
using Moq;

namespace Tests.Managers
{
    public class NotificationManagerTests
    {
        private readonly Mock<IProjectAccessAccessor> _projectAccess = new();
        private readonly Mock<INotificationAccessor> _notifications = new();

        [Fact]
        public async Task GetUnreadCountAsync_WithAccess_ReturnsRecipientCount()
        {
            _projectAccess
                .Setup(a => a.GetProjectAccessAsync(3, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProjectAccessRecord(1, "Omaha Fabrication", RoleNames.Foreman, []));
            _notifications.Setup(a => a.CountUnreadAsync(3, 1, It.IsAny<CancellationToken>())).ReturnsAsync(2);

            var result = await CreateManager().GetUnreadCountAsync(3, 1, CancellationToken.None);

            Assert.Equal(2, result.UnreadCount);
        }

        [Fact]
        public async Task GetUnreadCountAsync_NoProjectAccess_ThrowsWithoutCounting()
        {
            await Assert.ThrowsAsync<ProjectAccessDeniedException>(
                () => CreateManager().GetUnreadCountAsync(3, 2, CancellationToken.None));

            _notifications.Verify(a => a.CountUnreadAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(3, 0)]
        public async Task GetUnreadCountAsync_InvalidIds_ThrowsWithoutReading(int userId, int projectId)
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => CreateManager().GetUnreadCountAsync(userId, projectId, CancellationToken.None));

            _projectAccess.Verify(a => a.GetProjectAccessAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetNotificationsAsync_WithAccess_ReturnsNoticesAndUnreadTotal()
        {
            GrantAccess();
            _notifications
                .Setup(a => a.GetNotificationsAsync(3, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync([Notice(12, readAt: null), Notice(11, readAt: ReadTime)]);

            var result = await CreateManager().GetNotificationsAsync(3, 1, CancellationToken.None);

            Assert.Equal(1, result.UnreadCount);
            Assert.Equal([12, 11], result.Notices.Select(n => n.Id));
            Assert.Equal("delay.raised", result.Notices[0].EventType);
        }

        [Fact]
        public async Task GetNotificationsAsync_NoProjectAccess_ThrowsWithoutReading()
        {
            await Assert.ThrowsAsync<ProjectAccessDeniedException>(
                () => CreateManager().GetNotificationsAsync(3, 2, CancellationToken.None));

            _notifications.Verify(a => a.GetNotificationsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task MarkReadAsync_RecipientNotice_MarksReadAndReturnsNewTotal()
        {
            GrantAccess();
            _notifications
                .Setup(a => a.GetNotificationAsync(3, 1, 12, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Notice(12, readAt: ReadTime));
            _notifications.Setup(a => a.CountUnreadAsync(3, 1, It.IsAny<CancellationToken>())).ReturnsAsync(4);

            var result = await CreateManager().MarkReadAsync(3, 1, 12, CancellationToken.None);

            Assert.Equal(ReadTime, result.Notice.ReadAt);
            Assert.Equal(4, result.UnreadCount);
            _notifications.Verify(a => a.MarkReadAsync(
                3, 1, 12, It.Is<DateTimeOffset>(t => t.Offset == TimeSpan.Zero), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MarkReadAsync_NotRecipientNotice_ThrowsNotFound()
        {
            GrantAccess();

            await Assert.ThrowsAsync<NotificationNotFoundException>(
                () => CreateManager().MarkReadAsync(3, 1, 99, CancellationToken.None));
        }

        [Fact]
        public async Task MarkReadAsync_NoProjectAccess_ThrowsWithoutWriting()
        {
            await Assert.ThrowsAsync<ProjectAccessDeniedException>(
                () => CreateManager().MarkReadAsync(3, 2, 12, CancellationToken.None));

            VerifyNoReadWritten();
        }

        [Fact]
        public async Task MarkReadAsync_InvalidNotificationId_ThrowsWithoutWriting()
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => CreateManager().MarkReadAsync(3, 1, 0, CancellationToken.None));

            VerifyNoReadWritten();
        }

        private static readonly DateTimeOffset ReadTime = new(2026, 10, 7, 13, 0, 0, TimeSpan.Zero);

        private static NotificationRecord Notice(int id, DateTimeOffset? readAt) =>
            new(id, 1, "FN-1042", "Primer not cured", "delay.raised", new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero), readAt);

        private void GrantAccess() =>
            _projectAccess
                .Setup(a => a.GetProjectAccessAsync(3, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProjectAccessRecord(1, "Omaha Fabrication", RoleNames.Foreman, []));

        private void VerifyNoReadWritten() =>
            _notifications.Verify(a => a.MarkReadAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);

        private NotificationManager CreateManager() => new(_projectAccess.Object, _notifications.Object);
    }
}
