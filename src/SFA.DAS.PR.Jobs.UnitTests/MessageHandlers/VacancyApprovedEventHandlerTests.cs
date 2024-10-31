using Esfa.Recruit.Vacancies.Client.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.MessageHandlers.Recruit;
using SFA.DAS.PR.Jobs.Models.Recruit;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Jobs.UnitTests.MessageHandlers;

public sealed class VacancyApprovedEventHandlerTests
{
    private VacancyApprovedEventHandler _handler;
    private Mock<ILogger<VacancyApprovedEventHandler>> _loggerMock;
    private Mock<IAccountProviderLegalEntityRepository> _accountProviderLegalEntityRepositoryMock;
    private Mock<IRecruitApiClient> _recruitApiClientMock;
    private Mock<IProviderRelationshipsDataContext> _providerRelationshipsDataContextMock;
    private Mock<IAccountLegalEntityRepository> _accountLegalEntityRepositoryMock;
    private Mock<IAccountProviderRepository> _accountProviderRepositoryMock;
    private Mock<IProviderRepository> _providerRepositoryMock;
    private VacancyApprovedEvent _event;
    private Mock<IMessageHandlerContext> _messageHandlerContextMock;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<VacancyApprovedEventHandler>>();
        _accountProviderLegalEntityRepositoryMock = new Mock<IAccountProviderLegalEntityRepository>();
        _recruitApiClientMock = new Mock<IRecruitApiClient>();
        _providerRelationshipsDataContextMock = new Mock<IProviderRelationshipsDataContext>();
        _accountLegalEntityRepositoryMock = new Mock<IAccountLegalEntityRepository>();
        _accountProviderRepositoryMock = new Mock<IAccountProviderRepository>();
        _providerRepositoryMock = new Mock<IProviderRepository>();
        _messageHandlerContextMock = new Mock<IMessageHandlerContext>();

        _handler = new VacancyApprovedEventHandler(
            _loggerMock.Object,
            _accountProviderLegalEntityRepositoryMock.Object,
            _recruitApiClientMock.Object,
            _providerRelationshipsDataContextMock.Object,
            _accountLegalEntityRepositoryMock.Object,
            _accountProviderRepositoryMock.Object,
            _providerRepositoryMock.Object
        );

        _event = new VacancyApprovedEvent
        {
            VacancyReference = 123
        };
    }

    [Test]
    public async Task Handle_AccountLegalEntityNotFound_DoesNotProcessFurther()
    {
        var response = new LiveVacancyModel()
        {
            VacancyId = Guid.NewGuid(),
            AccountPublicHashedId = "apPublicHashedId",
            TrainingProvider = new TrainingProviderModel { Ukprn = 12345678 },
            AccountLegalEntityPublicHashedId = "aplePublicHashedId"
        };

        _recruitApiClientMock
            .Setup(x => x.GetLiveVacancy(_event.VacancyReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _accountLegalEntityRepositoryMock
            .Setup(x => x.GetAccountLegalEntity(response.AccountLegalEntityPublicHashedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountLegalEntity?)null);

        await _handler.Handle(_event, _messageHandlerContextMock.Object);

        _recruitApiClientMock.Verify(x => x.GetLiveVacancy(_event.VacancyReference, It.IsAny<CancellationToken>()), Times.Once);
        _accountLegalEntityRepositoryMock.Verify(x => x.GetAccountLegalEntity(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _providerRepositoryMock.Verify(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_ProviderNotFound_DoesNotProcessFurther()
    {
        var response = new LiveVacancyModel
        {
            VacancyId = Guid.NewGuid(),
            AccountPublicHashedId = "apPublicHashedId",
            TrainingProvider = new TrainingProviderModel { Ukprn = 12345678 },
            AccountLegalEntityPublicHashedId = "aplePublicHashedId"
        };

        var accountLegalEntity = new AccountLegalEntity { Id = 1, AccountId = 1 };

        _recruitApiClientMock
            .Setup(x => x.GetLiveVacancy(_event.VacancyReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _accountLegalEntityRepositoryMock
            .Setup(x => x.GetAccountLegalEntity(response.AccountLegalEntityPublicHashedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountLegalEntity);

        _providerRepositoryMock
            .Setup(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((long ukprn, CancellationToken token) => new ValueTask<Provider?>((Provider?)null));

        await _handler.Handle(_event, _messageHandlerContextMock.Object);

        _providerRepositoryMock.Verify(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
        _accountProviderRepositoryMock.Verify(x => x.GetAccountProvider(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_ProcessesVacancy_Successfully()
    {
        Account account = AccountData.Create(1);
        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(account, 1);
        accountLegalEntity.PublicHashedId = "aplePublicHashedId";

        AccountProviderLegalEntity accountProviderLegalEntity = AccountProviderLegalEntityData.CreateAple(account.Id, accountLegalEntity.Id);
        using var context = DbContextHelper
        .CreateInMemoryDbContext()
            .AddAccount(account)
            .AddAccountLegalEntity(accountLegalEntity)
            .AddAccountProviderLegalEntity(accountProviderLegalEntity)
            .PersistChanges();

        AccountProviderRepository accountProviderRepository = new AccountProviderRepository(context);
        AccountProviderLegalEntityRepository accountProviderLegalEntityRepository = new AccountProviderLegalEntityRepository(context);
        VacancyApprovedEventHandler _handler = new VacancyApprovedEventHandler(
            _loggerMock.Object,
            accountProviderLegalEntityRepository,
            _recruitApiClientMock.Object,
            context,
            _accountLegalEntityRepositoryMock.Object,
            accountProviderRepository,
            _providerRepositoryMock.Object
        );

        var response = new LiveVacancyModel
        {
            VacancyId = Guid.NewGuid(),
            AccountPublicHashedId = "apPublicHashedId",
            TrainingProvider = new TrainingProviderModel { Ukprn = 12345678 },
            AccountLegalEntityPublicHashedId = "aplePublicHashedId"
        };

        var provider = new Provider { Ukprn = 12345678 };

        _recruitApiClientMock
            .Setup(x => x.GetLiveVacancy(_event.VacancyReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _accountLegalEntityRepositoryMock
            .Setup(x => x.GetAccountLegalEntity(response.AccountLegalEntityPublicHashedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountLegalEntity);

        _providerRepositoryMock
            .Setup(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((long ukprn, CancellationToken token) => new ValueTask<Provider?>(provider));

        await _handler.Handle(_event, _messageHandlerContextMock.Object);

        var sut = await context.AccountProviderLegalEntities.FirstAsync(CancellationToken.None);
        var permissionAudit = await context.PermissionsAudit.FirstAsync(CancellationToken.None);
        var notification = await context.Notifications.FirstAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(sut, Is.Not.Null);

            Assert.That(permissionAudit, Is.Not.Null);
            Assert.That(permissionAudit.Action, Is.EqualTo(nameof(PermissionAction.RecruitRelationship)));
            Assert.That(permissionAudit.Ukprn, Is.EqualTo(provider.Ukprn));
            Assert.That(permissionAudit.Operations, Is.EqualTo("[]"));

            Assert.That(notification, Is.Not.Null);
            Assert.That(notification.Ukprn, Is.EqualTo(response.TrainingProvider.Ukprn));
            Assert.That(notification.CreatedBy, Is.EqualTo("PR Jobs: VacancyReviewedEvent"));
            Assert.That(notification.TemplateName, Is.EqualTo("LinkedAccountRecruit"));
            Assert.That(notification.NotificationType, Is.EqualTo(nameof(NotificationType.Provider)));
        });
    }
}
