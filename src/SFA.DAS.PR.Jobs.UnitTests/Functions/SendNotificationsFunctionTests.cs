using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.Notifications.Messages.Commands;
using SFA.DAS.PAS.Account.Api.Types;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Configuration;
using SFA.DAS.PR.Jobs.Functions.Notifications;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Services;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Jobs.UnitTests.Functions;

public class SendNotificationsFunctionTests
{
    private Mock<ILogger<SendNotificationsFunction>> _logger = new Mock<ILogger<SendNotificationsFunction>>();
    private Mock<IFunctionEndpoint> _functionEndpointMock = new Mock<IFunctionEndpoint>();
    private Mock<IPasAccountApiClient> _pasAccountApiClientMock = new Mock<IPasAccountApiClient>();
    private Mock<INotificationsConfiguration> _notificationsConfiguration = new Mock<INotificationsConfiguration>();

    [SetUp]
    public void SetUp()
    {
        _notificationsConfiguration.Setup(a => a.BatchSize).Returns(500);
        _notificationsConfiguration.Setup(a => a.NotificationTemplates).Returns(new List<TemplateConfiguration>()
        {
            new TemplateConfiguration()
            {
                TemplateName= "PermissionsCreated",
                TemplateId = "eddd2b41-ba27-4456-8558-bf207b924944"
            }
        });
    }

    [Test]
    public async Task SendEmployerLedNotificationsFunction_Run_UpdateSendTimeCorrectly()
    {
        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(1, 1);

        Notification notification = NotificationData.Create(
            Guid.NewGuid(), 
            NotificationType.Provider, 
            10011001, 
            accountLegalEntity.Id,
            "PermissionsCreated"
        );

        Provider provider = ProvidersData.Create();

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddProvider(provider)
            .AddAccountLegalEntity(accountLegalEntity)
            .AddNotification(notification)
            .PersistChanges();

        Mock<INotificationRepository> notificationRepositoryMock = new Mock<INotificationRepository>();
        notificationRepositoryMock.Setup(a => 
            a.GetPendingNotifications(_notificationsConfiguration.Object.BatchSize, NotificationType.Provider, It.IsAny<CancellationToken>())
        ).ReturnsAsync([notification]);

        Mock<IProvidersRepository> providersRepository = new Mock<IProvidersRepository>();
        providersRepository.Setup(a => 
            a.GetProvider(provider!.Ukprn, It.IsAny<CancellationToken>())
        ).ReturnsAsync(provider);

        Mock<IAccountLegalEntityRepository> accountLegalEntityRepository = new Mock<IAccountLegalEntityRepository>();
        accountLegalEntityRepository.Setup(a => 
            a.GetAccountLegalEntity(accountLegalEntity.Id, It.IsAny<CancellationToken>())
        ).ReturnsAsync(accountLegalEntity);

        SendNotificationsFunction sut = new(
            _logger.Object,
            SetupConfiguration(),
            _functionEndpointMock.Object,
            context,
            notificationRepositoryMock.Object,
            CreateTokenService(providersRepository, accountLegalEntityRepository),
            _pasAccountApiClientMock.Object,
            _notificationsConfiguration.Object
        );

        await sut.Run(
            new Mock<TimerInfo>().Object,
            new Mock<FunctionContext>().Object,
            CancellationToken.None
        );

        var updatedNotification = await context.Notifications.FirstOrDefaultAsync(a => a.Id == notification.Id, CancellationToken.None);

        using (new AssertionScope())
        {
            updatedNotification!.SentTime.Should().NotBeNull();
            updatedNotification!.SentTime!.Value.Date.Should().Be(DateTime.Now.Date);
            _pasAccountApiClientMock.Verify(a =>
                a.SendEmailToAllProviderRecipients(
                    It.IsAny<long>(),
                    It.IsAny<ProviderEmailRequest>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.AtLeastOnce
            );
        }
    }

    [Test]
    public async Task SendEmployerLedNotificationsFunction_Run_NoNotificationsProcessed()
    {
        using var context = DbContextHelper.CreateInMemoryDbContext().PersistChanges();

        Mock<INotificationRepository> notificationRepositoryMock = new Mock<INotificationRepository>();
        notificationRepositoryMock.Setup(a =>
            a.GetPendingNotifications(_notificationsConfiguration.Object.BatchSize, NotificationType.Provider, It.IsAny<CancellationToken>())
        ).ReturnsAsync([]);

        SendNotificationsFunction sut = new(
            _logger.Object,
            SetupConfiguration(),
            _functionEndpointMock.Object,
            context,
            notificationRepositoryMock.Object,
            CreateTokenService(new Mock<IProvidersRepository>(), new Mock<IAccountLegalEntityRepository>()),
            _pasAccountApiClientMock.Object,
            _notificationsConfiguration.Object
        );

        await sut.Run(
            new Mock<TimerInfo>().Object,
            new Mock<FunctionContext>().Object,
            CancellationToken.None
        );

        using (new AssertionScope())
        {
            _pasAccountApiClientMock.Verify(a =>
                a.SendEmailToAllProviderRecipients(
                    It.IsAny<long>(),
                    It.IsAny<ProviderEmailRequest>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never
            );
        }
    }

    [Test]
    public async Task SendEmployerLedNotificationsFunction_Run_Send_ThrowsArgumentNullException()
    {
        Notification notification = NotificationData.Create(
            Guid.NewGuid(),
            NotificationType.Provider,
            10011001,
            1,
            "PermissionsCreated"
        );

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddNotification(notification)
            .PersistChanges();

        Mock<INotificationRepository> notificationRepositoryMock = new Mock<INotificationRepository>();
        notificationRepositoryMock.Setup(a =>
            a.GetPendingNotifications(_notificationsConfiguration.Object.BatchSize, NotificationType.Provider, It.IsAny<CancellationToken>())
        ).ReturnsAsync([notification]);

        SendNotificationsFunction sut = new(
            _logger.Object,
            SetupConfiguration(),
            _functionEndpointMock.Object,
            context,
            notificationRepositoryMock.Object,
            CreateTokenService(new Mock<IProvidersRepository>(), new Mock<IAccountLegalEntityRepository>()),
            _pasAccountApiClientMock.Object,
            _notificationsConfiguration.Object
        );

        await sut.Run(
            new Mock<TimerInfo>().Object,
            new Mock<FunctionContext>().Object,
            CancellationToken.None
        );

        using (new AssertionScope())
        {
            _pasAccountApiClientMock.Verify(a =>
                a.SendEmailToAllProviderRecipients(
                    It.IsAny<long>(),
                    It.IsAny<ProviderEmailRequest>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never
            );
        }
    }

    private IConfiguration SetupConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"ApplicationConfiguration:Notifications:BatchSize", _notificationsConfiguration.Object.BatchSize.ToString()},
            {"ApplicationConfiguration:Notifications:NotificationTemplates:0:TemplateName", _notificationsConfiguration.Object.NotificationTemplates[0].TemplateName},
            {"ApplicationConfiguration:Notifications:NotificationTemplates:0:TemplateId", _notificationsConfiguration.Object.NotificationTemplates[0].TemplateId}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        return configuration;
    }

    private static NotificationTokenService CreateTokenService(Mock<IProvidersRepository> providersRepository, Mock<IAccountLegalEntityRepository> accountLegalEntityRepository)
    {
        return new NotificationTokenService(
            providersRepository.Object, 
            accountLegalEntityRepository.Object
        );
    }
}
 