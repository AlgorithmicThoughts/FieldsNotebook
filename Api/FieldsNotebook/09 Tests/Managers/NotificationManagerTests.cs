using Accessors.Notifications;
using Accessors.Projects;
using Common.Errors;
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

        private NotificationManager CreateManager() => new(_projectAccess.Object, _notifications.Object);
    }
}
