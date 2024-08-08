using AutoFixture.NUnit3;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Configuration;
using SFA.DAS.PR.Jobs.Services;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Jobs.UnitTests.Services;

public class NotificationTokenServiceTests
{
    [Test, AutoData]
    public async Task NotificationTokenService_GetEmailTokens_Returns_Correct_Tokens(NotificationsConfiguration notificationsConfiguration)
    {
        Provider provider = ProvidersData.Create();

        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(1, 1);

        Notification notification = NotificationData.Create(
            Guid.NewGuid(),
            NotificationType.Provider,
            provider.Ukprn,
            accountLegalEntity.Id,
            "PermissionsCreated",
            1,
            1
        );

        Mock<IProvidersRepository> providerRepositoryMock = new Mock<IProvidersRepository>();
        providerRepositoryMock.Setup(a => a.GetProvider(provider.Ukprn, It.IsAny<CancellationToken>())).ReturnsAsync(provider);

        Mock<IAccountLegalEntityRepository> accountLegalEntityRepositoryMock = new Mock<IAccountLegalEntityRepository>();
        accountLegalEntityRepositoryMock.Setup(a => a.GetAccountLegalEntity(accountLegalEntity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(accountLegalEntity);

        Mock<IOptions<NotificationsConfiguration>> notificationsConfigurationOptionsMock = new();
        notificationsConfigurationOptionsMock.Setup(o => o.Value).Returns(notificationsConfiguration);

        NotificationTokenService sut = new NotificationTokenService(providerRepositoryMock.Object, accountLegalEntityRepositoryMock.Object, notificationsConfigurationOptionsMock.Object);

        var actualTokens = await sut.GetEmailTokens(notification, CancellationToken.None);

        var expectedTokens = new Dictionary<string, string>()
        {
            { EmailTokens.ProviderPortalUrlToken, notificationsConfiguration.ProviderPortalUrl},
            { EmailTokens.ProviderNameToken, provider!.Name },
            { EmailTokens.EmployerNameToken, accountLegalEntity!.Name },
            { EmailTokens.PermitRecruitToken, "create and publish" },
            { EmailTokens.PermitApprovalsToken, "add" }
        };

        actualTokens.Should().BeEquivalentTo(expectedTokens);
    }

    [Test, AutoData]
    public async Task NotificationTokenService_GetEmailTokens_Invalid_ReturnsNull(NotificationsConfiguration notificationsConfiguration)
    {
        Provider provider = ProvidersData.Create();

        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(1, 1);

        Notification notification = NotificationData.Create(
            Guid.NewGuid(),
            NotificationType.Provider,
            provider.Ukprn,
            accountLegalEntity.Id,
            "PermissionsCreated",
            -1,
            -1
        );

        Mock<IProvidersRepository> providerRepositoryMock = new Mock<IProvidersRepository>();
        providerRepositoryMock.Setup(a => a.GetProvider(provider.Ukprn, It.IsAny<CancellationToken>())).ReturnsAsync(provider);

        Mock<IAccountLegalEntityRepository> accountLegalEntityRepositoryMock = new Mock<IAccountLegalEntityRepository>();
        accountLegalEntityRepositoryMock.Setup(a => a.GetAccountLegalEntity(accountLegalEntity.Id, It.IsAny<CancellationToken>())).ReturnsAsync(accountLegalEntity);

        Mock<IOptions<NotificationsConfiguration>> notificationsConfigurationOptionsMock = new();
        notificationsConfigurationOptionsMock.Setup(o => o.Value).Returns(notificationsConfiguration);

        NotificationTokenService sut = new NotificationTokenService(providerRepositoryMock.Object, accountLegalEntityRepositoryMock.Object, notificationsConfigurationOptionsMock.Object);

        var tokens = await sut.GetEmailTokens(notification, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(tokens[EmailTokens.PermitRecruitToken], Is.Null);
            Assert.That(tokens[EmailTokens.PermitApprovalsToken], Is.Null);
        });
    }
}
