using AutoFixture.NUnit3;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SFA.DAS.Encoding;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Configuration;
using SFA.DAS.PR.Jobs.Services;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Jobs.UnitTests.Services;

public class NotificationTokenServiceTests
{
    private const string ProviderPortalURL = "https://provider.portal";
    private const string ProviderPRBaseUrl = "https://provider.web";
    private const string EmployerPRBaseUrl = "https://employer.web";
    private const string EmployerAccountsBaseUrl = "https://employer.accounts";
    private const int RequestExpiry = 30;

    private Mock<IProvidersRepository> _providersRepositoryMock;
    private Mock<IAccountLegalEntityRepository> _accountLegalEntityRepositoryMock;
    private Mock<IRequestsRepository> _requestsRepositoryMock;
    private Mock<IOptions<NotificationsConfiguration>> _notificationConfigurationOptionsMock;

    private NotificationTokenService _notificationTokenService;

    [SetUp]
    public void Setup()
    {
        _providersRepositoryMock = new Mock<IProvidersRepository>();
        _accountLegalEntityRepositoryMock = new Mock<IAccountLegalEntityRepository>();
        _requestsRepositoryMock = new Mock<IRequestsRepository>();
        _notificationConfigurationOptionsMock = new Mock<IOptions<NotificationsConfiguration>>();

        _notificationConfigurationOptionsMock.Setup(x => x.Value).Returns(new NotificationsConfiguration
        {
            ProviderPortalUrl = ProviderPortalURL,
            ProviderPRBaseUrl = ProviderPRBaseUrl,
            EmployerPRBaseUrl = EmployerPRBaseUrl,
            EmployerAccountsBaseUrl = EmployerAccountsBaseUrl,
            RequestExpiry = RequestExpiry
        });

        _notificationTokenService = new NotificationTokenService(
            _providersRepositoryMock.Object,
            _accountLegalEntityRepositoryMock.Object,
            _requestsRepositoryMock.Object,
            _notificationConfigurationOptionsMock.Object
        );
    }

    [Test]
    public async Task NotificationTokenService_GetEmailTokens_ShouldAddProviderTokens_WhenProviderExists()
    {
        var notification = new Notification
        {
            Ukprn = 12345678,
            NotificationType = NotificationType.Provider.ToString(),
            TemplateName = "TemplateName",
            CreatedBy = Guid.NewGuid().ToString()
        };

        var provider = new Provider
        {
            Name = "Test Provider",
            Ukprn = 12345678
        };

        _providersRepositoryMock
            .Setup(repo => repo.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var result = await _notificationTokenService.GetEmailTokens(notification, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.ContainsKey(NotificationTokens.ProviderName), Is.True);
            Assert.That(result.ContainsKey(NotificationTokens.Ukprn), Is.True);
            Assert.That(result[NotificationTokens.ProviderName], Is.EqualTo("Test Provider"));
            Assert.That(result[NotificationTokens.Ukprn], Is.EqualTo("12345678"));
        });
    }

    [Test]
    public async Task NotificationTokenService_GetEmailTokens_ShouldAddEmployerTokens_WhenAccountLegalEntityExists()
    {
        var notification = new Notification
        {
            AccountLegalEntityId = 1,
            NotificationType = NotificationType.Employer.ToString(),
            TemplateName = "TemplateName",
            CreatedBy = Guid.NewGuid().ToString()
        };

        var accountLegalEntity = new AccountLegalEntity
        {
            Name = "Test Employer",
            Id = 1,
            Account = new Account { PublicHashedId = "ABC123" },
            PublicHashedId = "PublicHashedId"
        };

        _accountLegalEntityRepositoryMock
            .Setup(repo => repo.GetAccountLegalEntity(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountLegalEntity);

        var result = await _notificationTokenService.GetEmailTokens(notification, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.ContainsKey(NotificationTokens.EmployerName), Is.True);
            Assert.That(result[NotificationTokens.EmployerName], Is.EqualTo("Test Employer"));
            Assert.That(result.ContainsKey(NotificationTokens.AccountLegalEntityHashedId), Is.True);
            Assert.That(result[NotificationTokens.AccountLegalEntityHashedId], Is.EqualTo("PublicHashedId"));
            Assert.That(result.ContainsKey(NotificationTokens.AccountHashedId), Is.True);
            Assert.That(result[NotificationTokens.AccountHashedId], Is.EqualTo("ABC123"));
        });
    }

    [Test]
    public async Task NotificationTokenService_GetEmailTokens_ShouldAddOrganisationName_WhenAccountLegalEntityDoesNotExists()
    {
        Request request = RequestData.Create(Guid.NewGuid());

        var notification = new Notification
        {
            NotificationType = NotificationType.Employer.ToString(),
            TemplateName = "TemplateName",
            CreatedBy = Guid.NewGuid().ToString(),
            RequestId = Guid.NewGuid()
        };

        _requestsRepositoryMock
            .Setup(repo => repo.GetRequest(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken token) => new ValueTask<Request?>(request));

        _accountLegalEntityRepositoryMock
            .Setup(repo => repo.GetAccountLegalEntity(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountLegalEntity?)null);

        var result = await _notificationTokenService.GetEmailTokens(notification, CancellationToken.None);

        Assert.That(result[NotificationTokens.EmployerName], Is.EqualTo(request.EmployerOrganisationName));
    }

    [Test]
    public async Task NotificationTokenService_GetEmailTokens_AddNotificationSpecificTokens_Invalid()
    {
        Request request = RequestData.Create(Guid.NewGuid());

        var notification = new Notification
        {
            NotificationType = "Invalid",
            TemplateName = "TemplateName",
            CreatedBy = Guid.NewGuid().ToString(),
            RequestId = Guid.NewGuid()
        };

        _requestsRepositoryMock
            .Setup(repo => repo.GetRequest(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, CancellationToken token) => new ValueTask<Request?>(request));

        _accountLegalEntityRepositoryMock
            .Setup(repo => repo.GetAccountLegalEntity(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountLegalEntity?)null);

        var result = await _notificationTokenService.GetEmailTokens(notification, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.ContainsKey(NotificationTokens.ProviderPortalUrl), Is.False);
            Assert.That(result.ContainsKey(NotificationTokens.RequestExpiry), Is.False);
        });
    }

    [Test]
    public async Task NotificationTokenService_GetEmailTokens_ShouldAddNotificationSpecificTokens_ForProviderNotification()
    {
        var notification = new Notification
        {
            Ukprn = 12345678,
            NotificationType = NotificationType.Provider.ToString(),
            PermitRecruit = 1,
            PermitApprovals = 0,
            TemplateName = "TemplateName",
            CreatedBy = Guid.NewGuid().ToString()
        };

        var provider = new Provider
        {
            Name = "Test Provider",
            Ukprn = 12345678
        };

        _providersRepositoryMock
            .Setup(repo => repo.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var result = await _notificationTokenService.GetEmailTokens(notification, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.ContainsKey(NotificationTokens.ProviderPortalUrl), Is.True);
            Assert.That(result[NotificationTokens.ProviderPortalUrl], Is.EqualTo(ProviderPortalURL));
            Assert.That(result.ContainsKey(NotificationTokens.PermitRecruit), Is.True);
            Assert.That(result[NotificationTokens.PermitRecruit], Is.EqualTo(NotificationTokens.RecruitCreateAndPublish));
            Assert.That(result.ContainsKey(NotificationTokens.PermitApprovals), Is.True);
            Assert.That(result[NotificationTokens.PermitApprovals], Is.EqualTo(NotificationTokens.ApprovalsCannotAdd));
        });
    }
}
