using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private Mock<IPasAccountApiClient> _pasAccountApiClientMock = new Mock<IPasAccountApiClient>();
    private Mock<IRequestsRepository> _requestsRepositoryMock = new Mock<IRequestsRepository>();
    private Mock<IOptions<NotificationsConfiguration>> _notificationsConfigurationOptionsMock = new();
    private NotificationsConfiguration _notificationsConfiguration = new();

    [SetUp]
    public void SetUp()
    {
        _notificationsConfiguration.ProviderPortalUrl = "https://provider.portal.com";
        _notificationsConfiguration.BatchSize = 500;
        _notificationsConfiguration.EmployerPRBaseUrl = "EmployerPRBaseUrl";
        _notificationsConfiguration.EmployerAccountsBaseUrl = "EmployerAccountsBaseUrl";
        _notificationsConfiguration.NotificationTemplates =
        [
            new TemplateConfiguration()
            {
                TemplateName= "PermissionsCreated",
                TemplateId = "eddd2b41-ba27-4456-8558-bf207b924944"
            }
        ];

        _notificationsConfigurationOptionsMock.Setup(o => o.Value).Returns(_notificationsConfiguration);
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
            a.GetPendingNotifications(_notificationsConfiguration.BatchSize, It.IsAny<CancellationToken>())
        ).ReturnsAsync([notification]);

        Mock<IProvidersRepository> providersRepository = new Mock<IProvidersRepository>();
        providersRepository
            .Setup(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((long ukprn, CancellationToken token) => new ValueTask<Provider?>(provider));

        Mock<IAccountLegalEntityRepository> accountLegalEntityRepository = new Mock<IAccountLegalEntityRepository>();
        accountLegalEntityRepository.Setup(a =>
            a.GetAccountLegalEntity(accountLegalEntity.Id, It.IsAny<CancellationToken>())
        ).ReturnsAsync(accountLegalEntity);

        Mock<IFunctionEndpoint> functionEndpointMock = new Mock<IFunctionEndpoint>();
        Mock<IRequestsRepository> requestsRepositoryMock = new Mock<IRequestsRepository>();

        SendNotificationsFunction sut = new(
            _logger.Object,
            context,
            notificationRepositoryMock.Object,
            CreateNotificationTokenService(providersRepository, accountLegalEntityRepository),
            _pasAccountApiClientMock.Object,
            functionEndpointMock.Object,
            requestsRepositoryMock.Object,
            _notificationsConfigurationOptionsMock.Object
        );

        await sut.Run(new Mock<TimerInfo>().Object, new Mock<FunctionContext>().Object, CancellationToken.None);

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
            a.GetPendingNotifications(_notificationsConfiguration.BatchSize, It.IsAny<CancellationToken>())
        ).ReturnsAsync([]);

        Mock<IFunctionEndpoint> functionEndpointMock = new Mock<IFunctionEndpoint>();
        Mock<IRequestsRepository> requestsRepositoryMock = new Mock<IRequestsRepository>();

        SendNotificationsFunction sut = new(
            _logger.Object,
            context,
            notificationRepositoryMock.Object,
            CreateNotificationTokenService(new Mock<IProvidersRepository>(), new Mock<IAccountLegalEntityRepository>()),
            _pasAccountApiClientMock.Object,
            functionEndpointMock.Object,
            requestsRepositoryMock.Object,
            _notificationsConfigurationOptionsMock.Object
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
            "InvalidTemplate"
        );

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddNotification(notification)
            .PersistChanges();

        Mock<INotificationRepository> notificationRepositoryMock = new Mock<INotificationRepository>();
        notificationRepositoryMock.Setup(a =>
            a.GetPendingNotifications(_notificationsConfiguration.BatchSize, It.IsAny<CancellationToken>())
        ).ReturnsAsync([notification]);

        Mock<IFunctionEndpoint> functionEndpointMock = new Mock<IFunctionEndpoint>();
        Mock<IRequestsRepository> requestsRepositoryMock = new Mock<IRequestsRepository>();

        SendNotificationsFunction sut = new(
            _logger.Object,
            context,
            notificationRepositoryMock.Object,
            CreateNotificationTokenService(new Mock<IProvidersRepository>(), new Mock<IAccountLegalEntityRepository>()),
            _pasAccountApiClientMock.Object,
            functionEndpointMock.Object,
            requestsRepositoryMock.Object,
            _notificationsConfigurationOptionsMock.Object
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
                    notification.Ukprn!.Value,
                    It.IsAny<ProviderEmailRequest>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Never
            );
        }
    }

    [Test]
    public async Task SendEmployerLedNotificationsFunction_Run_NullUukprnNotification_GetFromRequest()
    {
        Guid requestId = Guid.NewGuid();

        Notification notification = NotificationData.Create(
            Guid.NewGuid(),
            NotificationType.Provider,
            null,
            1,
            "PermissionsCreated",
            requestId: requestId
        );

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddNotification(notification)
            .AddRequest(requestId, RequestStatus.New)
            .PersistChanges();

        Mock<INotificationRepository> notificationRepositoryMock = new Mock<INotificationRepository>();
        notificationRepositoryMock.Setup(a =>
            a.GetPendingNotifications(_notificationsConfiguration.BatchSize, It.IsAny<CancellationToken>())
        ).ReturnsAsync([notification]);

        Mock<IFunctionEndpoint> functionEndpointMock = new Mock<IFunctionEndpoint>();

        RequestsRepository requestRepository = new RequestsRepository(context);

        SendNotificationsFunction sut = new(
            _logger.Object,
            context,
            notificationRepositoryMock.Object,
            CreateNotificationTokenService(new Mock<IProvidersRepository>(), new Mock<IAccountLegalEntityRepository>()),
            _pasAccountApiClientMock.Object,
            functionEndpointMock.Object,
            requestRepository,
            _notificationsConfigurationOptionsMock.Object
        );

        await sut.Run(
            new Mock<TimerInfo>().Object,
            new Mock<FunctionContext>().Object,
            CancellationToken.None
        );

        var request = context.Requests.First(a => a.Id == requestId);

        using (new AssertionScope())
        {
            _pasAccountApiClientMock.Verify(a =>
                a.SendEmailToAllProviderRecipients(
                    request.Ukprn,
                    It.IsAny<ProviderEmailRequest>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );

            Assert.That(request.Status, Is.EqualTo(RequestStatus.Sent));
        }
    }

    [Test]
    public async Task SendNotificationsFunction_Run_ActionedRequest_RetainRequestStatus()
    {
        Guid requestId = Guid.NewGuid();

        Notification notification = NotificationData.Create(
            Guid.NewGuid(),
            NotificationType.Provider,
            null,
            1,
            "PermissionsCreated",
            requestId: requestId
        );

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddNotification(notification)
            .AddRequest(requestId, RequestStatus.Accepted)
            .PersistChanges();

        Mock<INotificationRepository> notificationRepositoryMock = new Mock<INotificationRepository>();
        notificationRepositoryMock.Setup(a =>
            a.GetPendingNotifications(_notificationsConfiguration.BatchSize, It.IsAny<CancellationToken>())
        ).ReturnsAsync([notification]);

        Mock<IFunctionEndpoint> functionEndpointMock = new Mock<IFunctionEndpoint>();

        RequestsRepository requestRepository = new RequestsRepository(context);

        SendNotificationsFunction sut = new(
            _logger.Object,
            context,
            notificationRepositoryMock.Object,
            CreateNotificationTokenService(new Mock<IProvidersRepository>(), new Mock<IAccountLegalEntityRepository>()),
            _pasAccountApiClientMock.Object,
            functionEndpointMock.Object,
            requestRepository,
            _notificationsConfigurationOptionsMock.Object
        );

        await sut.Run(
            new Mock<TimerInfo>().Object,
            new Mock<FunctionContext>().Object,
            CancellationToken.None
        );

        var request = context.Requests.First(a => a.Id == requestId);

        using (new AssertionScope())
        {
            Assert.That(request.Status, Is.EqualTo(RequestStatus.Accepted));
        }
    }

    [Test]
    public async Task SendEmployerLedNotificationsFunction_Run_EmployerRoute()
    {
        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(1, 1);

        Notification notification = NotificationData.Create(
            Guid.NewGuid(),
            NotificationType.Employer,
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
            a.GetPendingNotifications(_notificationsConfiguration.BatchSize, It.IsAny<CancellationToken>())
        ).ReturnsAsync([notification]);

        Mock<IProvidersRepository> providersRepository = new Mock<IProvidersRepository>();
        providersRepository
            .Setup(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((long ukprn, CancellationToken token) => new ValueTask<Provider?>(provider));

        Mock<IAccountLegalEntityRepository> accountLegalEntityRepository = new Mock<IAccountLegalEntityRepository>();
        accountLegalEntityRepository.Setup(a =>
            a.GetAccountLegalEntity(accountLegalEntity.Id, It.IsAny<CancellationToken>())
        ).ReturnsAsync(accountLegalEntity);

        Mock<IFunctionEndpoint> functionEndpointMock = new Mock<IFunctionEndpoint>();
        Mock<IRequestsRepository> requestsRepositoryMock = new Mock<IRequestsRepository>();

        SendNotificationsFunction sut = new(
            _logger.Object,
            context,
            notificationRepositoryMock.Object,
            CreateNotificationTokenService(providersRepository, accountLegalEntityRepository),
            _pasAccountApiClientMock.Object,
            functionEndpointMock.Object,
            requestsRepositoryMock.Object,
            _notificationsConfigurationOptionsMock.Object
        );

        await sut.Run(new Mock<TimerInfo>().Object, new Mock<FunctionContext>().Object, CancellationToken.None);

        var updatedNotification = await context.Notifications.FirstOrDefaultAsync(a => a.Id == notification.Id, CancellationToken.None);

        using (new AssertionScope())
        {
            updatedNotification!.SentTime.Should().NotBeNull();
            updatedNotification!.SentTime!.Value.Date.Should().Be(DateTime.Now.Date);
            functionEndpointMock.Verify(a =>
                a.Send(
                    It.Is<SendEmailCommand>(a =>
                        a.RecipientsAddress == notification.EmailAddress
                    ),
                    It.IsAny<FunctionContext>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.AtLeastOnce
            );
        }
    }

    [Test]
    public async Task SendEmployerLedNotificationsFunction_Run_SetsNotificationUkprn()
    {
        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(1, 1);

        Guid requestId = Guid.NewGuid();

        Notification notification = NotificationData.Create(
            Guid.NewGuid(),
            NotificationType.Employer,
            null,
            accountLegalEntity.Id,
            "PermissionsCreated",
            requestId: requestId
        );

        Provider provider = ProvidersData.Create();

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddProvider(provider)
            .AddAccountLegalEntity(accountLegalEntity)
            .AddNotification(notification)
            .AddRequest(requestId)
            .PersistChanges();

        var request = context.Requests.First();

        Mock<INotificationRepository> notificationRepositoryMock = new Mock<INotificationRepository>();
        notificationRepositoryMock.Setup(a =>
            a.GetPendingNotifications(_notificationsConfiguration.BatchSize, It.IsAny<CancellationToken>())
        ).ReturnsAsync([notification]);

        Mock<IProvidersRepository> providersRepository = new Mock<IProvidersRepository>();
        providersRepository
            .Setup(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((long ukprn, CancellationToken token) => new ValueTask<Provider?>(provider));

        Mock<IAccountLegalEntityRepository> accountLegalEntityRepository = new Mock<IAccountLegalEntityRepository>();
        accountLegalEntityRepository.Setup(a =>
            a.GetAccountLegalEntity(accountLegalEntity.Id, It.IsAny<CancellationToken>())
        ).ReturnsAsync(accountLegalEntity);

        Mock<IFunctionEndpoint> functionEndpointMock = new Mock<IFunctionEndpoint>();
        RequestsRepository requestsRepository = new RequestsRepository(context);

        SendNotificationsFunction sut = new(
            _logger.Object,
            context,
            notificationRepositoryMock.Object,
            CreateNotificationTokenService(providersRepository, accountLegalEntityRepository),
            _pasAccountApiClientMock.Object,
            functionEndpointMock.Object,
            requestsRepository,
            _notificationsConfigurationOptionsMock.Object
        );

        await sut.Run(new Mock<TimerInfo>().Object, new Mock<FunctionContext>().Object, CancellationToken.None);

        var updatedNotification = await context.Notifications.FirstOrDefaultAsync(a => a.Id == notification.Id, CancellationToken.None);

        using (new AssertionScope())
        {
            updatedNotification!.Ukprn.Should().Be(request.Ukprn);
        }
    }

    private NotificationTokenService CreateNotificationTokenService(Mock<IProvidersRepository> providersRepository, Mock<IAccountLegalEntityRepository> accountLegalEntityRepository)
    {
        return new NotificationTokenService(
            providersRepository.Object,
            accountLegalEntityRepository.Object,
            _requestsRepositoryMock.Object,
            _notificationsConfigurationOptionsMock.Object
        );
    }
}
