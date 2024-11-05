using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;
using Moq;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Configuration;
using SFA.DAS.PR.Jobs.Functions.Notifications;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Jobs.UnitTests.Functions.Notifications
{
    public class NotificationsCleanUpFunctionTests
    {
        private Mock<INotificationRepository> _notificationRepositoryMock;
        private Mock<IProviderRelationshipsDataContext> _dataContextMock;
        private NotificationsCleanUpFunction _function;
        private NotificationsConfiguration _notificationsConfig;
        private TimerInfo _timerInfo;
        private FunctionContext _functionContext;

        [SetUp]
        public void SetUp()
        {
            _notificationRepositoryMock = new Mock<INotificationRepository>();
            _dataContextMock = new Mock<IProviderRelationshipsDataContext>();
            _notificationsConfig = new NotificationsConfiguration { NotificationRetentionDays = 30 };
            var configOptions = Options.Create(_notificationsConfig);

            _function = new NotificationsCleanUpFunction(
                _notificationRepositoryMock.Object,
                _dataContextMock.Object,
                configOptions
            );

            _timerInfo = new TimerInfo();
            _functionContext = new Mock<FunctionContext>().Object;
        }

        [Test]
        public async Task Run_NoExpiredNotifications_DoesNotCallDeleteOrSaveChanges()
        {
            _notificationRepositoryMock
                .Setup(repo => repo.GetExpiredNotifications(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Notification>()); // No expired notifications

            await _function.Run(_timerInfo, _functionContext, CancellationToken.None);

            _notificationRepositoryMock.Verify(repo => 
                repo.DeleteNotifications(
                    It.IsAny<List<Notification>>()
                ), 
                Times.Never
            );

            _dataContextMock.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Run_ExpiredNotifications_CallsDeleteAndSaveChanges()
        {
            var notification = NotificationData.Create(Guid.NewGuid(), NotificationType.Provider, 10000001, 1, "TemplateName");
            var expiredNotifications = new List<Notification> { notification };
            _notificationRepositoryMock
                .Setup(repo => repo.GetExpiredNotifications(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expiredNotifications);

            await _function.Run(_timerInfo, _functionContext, CancellationToken.None);

            _notificationRepositoryMock.Verify(repo => repo.DeleteNotifications(expiredNotifications), Times.Once);
            _dataContextMock.Verify(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
