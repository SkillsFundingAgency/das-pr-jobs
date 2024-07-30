using Moq;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Services;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Jobs.UnitTests.Services;

public class NotificationTokenServiceTests
{
    [Test]
    public async Task NotificationTokenService_GetEmailTokens_Returns_Correct_Tokens()
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

        NotificationTokenService sut = new NotificationTokenService(providerRepositoryMock.Object, accountLegalEntityRepositoryMock.Object);

        var tokens = await sut.GetEmailTokens(notification, CancellationToken.None);

        var expectedTokens = new Dictionary<string, string>()
        {
            { EmailTokens.ProviderNameToken, provider!.Name },
            { EmailTokens.EmployerNameToken, accountLegalEntity!.Name },
            { EmailTokens.PermitRecruitToken, "create and publish" },
            { EmailTokens.PermitApprovalsToken, "add" }
        };

        Assert.Multiple(() => {
            Assert.That(expectedTokens[EmailTokens.ProviderNameToken], Is.EqualTo(tokens[EmailTokens.ProviderNameToken]));
            Assert.That(expectedTokens[EmailTokens.EmployerNameToken], Is.EqualTo(tokens[EmailTokens.EmployerNameToken]));
            Assert.That(expectedTokens[EmailTokens.PermitRecruitToken], Is.EqualTo(tokens[EmailTokens.PermitRecruitToken]));
            Assert.That(expectedTokens[EmailTokens.PermitApprovalsToken], Is.EqualTo(tokens[EmailTokens.PermitApprovalsToken]));
        });
    }

    [Test]
    public async Task NotificationTokenService_GetEmailTokens_Invalid_ReturnsNull()
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

        NotificationTokenService sut = new NotificationTokenService(providerRepositoryMock.Object, accountLegalEntityRepositoryMock.Object);

        var tokens = await sut.GetEmailTokens(notification, CancellationToken.None);

        Assert.Multiple(() => {
            Assert.That(tokens[EmailTokens.PermitRecruitToken], Is.Null);
            Assert.That(tokens[EmailTokens.PermitApprovalsToken], Is.Null);
        });
    }
}
