using Esfa.Recruit.Vacancies.Client.Domain.Events;
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
using System.Text.Json;

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
    private Mock<IJobAuditRepository> _jobAuditRepository;

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
        _jobAuditRepository = new Mock<IJobAuditRepository>();

        _handler = new VacancyApprovedEventHandler(
            _loggerMock.Object,
            _accountProviderLegalEntityRepositoryMock.Object,
            _recruitApiClientMock.Object,
            _providerRelationshipsDataContextMock.Object,
            _accountLegalEntityRepositoryMock.Object,
            _accountProviderRepositoryMock.Object,
            _providerRepositoryMock.Object,
            _jobAuditRepository.Object
        );

        _event = new VacancyApprovedEvent
        {
            VacancyReference = 123
        };
    }

    [Test]
    public async Task Handle_AccountLegalEntityNotFound_DoesNotProcessFurther()
    {
        var response = CreateLiveVacanyModel();

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
    public async Task Handle_VacancyApprovedEvent_AccountProviderLegalEntityIsNotNull_Returns()
    {
        Account account = AccountData.Create(1);
        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(account, 1);
        accountLegalEntity.PublicHashedId = "aplePublicHashedId";

        using var context = DbContextHelper
        .CreateInMemoryDbContext()
            .AddAccount(account)
            .AddAccountLegalEntity(accountLegalEntity)
            .PersistChanges();

        AccountProviderRepository accountProviderRepository = new AccountProviderRepository(context);
        JobAuditRepository jobAuditRepository = new JobAuditRepository(context);
        AccountProviderLegalEntityRepository accountProviderLegalEntityRepository = new AccountProviderLegalEntityRepository(context);
        VacancyApprovedEventHandler _handler = new VacancyApprovedEventHandler(
            _loggerMock.Object,
            accountProviderLegalEntityRepository,
            _recruitApiClientMock.Object,
            context,
            _accountLegalEntityRepositoryMock.Object,
            accountProviderRepository,
            _providerRepositoryMock.Object,
            jobAuditRepository
        );

        var response = CreateLiveVacanyModel();

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

        var sut = context.AccountProviderLegalEntities.First();
        var permissionAudit = context.PermissionsAudit.First();
        var notification = context.Notifications.First();
        var jobAudit = context.JobAudits.First();

        Assert.Multiple(() =>
        {
            Assert.That(sut, Is.Not.Null);

            Assert.That(permissionAudit, Is.Not.Null);
            Assert.That(permissionAudit.Action, Is.EqualTo(nameof(PermissionAction.RecruitRelationship)));
            Assert.That(permissionAudit.Ukprn, Is.EqualTo(provider.Ukprn));
            Assert.That(permissionAudit.Operations, Is.EqualTo("[]"));

            Assert.That(notification, Is.Not.Null);
            Assert.That(notification.Ukprn, Is.EqualTo(response.TrainingProvider!.Ukprn));
            Assert.That(notification.CreatedBy, Is.EqualTo("System"));
            Assert.That(notification.TemplateName, Is.EqualTo("LinkedAccountRecruit"));
            Assert.That(notification.NotificationType, Is.EqualTo(nameof(NotificationType.Provider)));

            Assert.That(jobAudit, Is.Not.Null);
            Assert.That(jobAudit.JobName, Is.EqualTo(nameof(VacancyApprovedEvent)));
            Assert.That(jobAudit.JobInfo, Is.EqualTo($"{JsonSerializer.Serialize(_event)}"));
        });
    }

    [Test]
    public async Task Handle_AccountProviderLegalEntityIsNotNull_ExitsEventProcessing()
    {
        AccountProviderLegalEntity accountProviderLegalEntity = AccountProviderLegalEntityData.Create(1, 1);

        using var context = DbContextHelper
        .CreateInMemoryDbContext()
            .AddAccountProviderLegalEntity(accountProviderLegalEntity)
            .PersistChanges();

        Mock<IAccountProviderRepository> _accountProviderRepositoryMock = new Mock<IAccountProviderRepository>();
        Mock<IJobAuditRepository> jobAuditRepository = new Mock<IJobAuditRepository>();
        Mock<IPermissionAuditRepository> permissionAuditRepository = new Mock<IPermissionAuditRepository>();

        Mock<IAccountProviderLegalEntityRepository> accountProviderLegalEntityRepository = new Mock<IAccountProviderLegalEntityRepository>();
        accountProviderLegalEntityRepository.Setup(a =>
            a.GetAccountProviderLegalEntity(
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()
            )
        ).ReturnsAsync(accountProviderLegalEntity);

        VacancyApprovedEventHandler _handler = new VacancyApprovedEventHandler(
            _loggerMock.Object,
            accountProviderLegalEntityRepository.Object,
            _recruitApiClientMock.Object,
            context,
            _accountLegalEntityRepositoryMock.Object,
            _accountProviderRepositoryMock.Object,
            _providerRepositoryMock.Object,
            jobAuditRepository.Object
        );

        var response = CreateLiveVacanyModel();

        var provider = new Provider { Ukprn = 12345678 };

        _recruitApiClientMock
            .Setup(x => x.GetLiveVacancy(_event.VacancyReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _accountLegalEntityRepositoryMock
            .Setup(x => x.GetAccountLegalEntity(response.AccountLegalEntityPublicHashedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountProviderLegalEntity.AccountLegalEntity);

        _accountProviderRepositoryMock
            .Setup(x => x.GetAccountProvider(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((long providerUkprn, long accountId, CancellationToken cancellationToken) => new ValueTask<AccountProvider?>(accountProviderLegalEntity.AccountProvider));

        _providerRepositoryMock
            .Setup(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((long ukprn, CancellationToken token) => new ValueTask<Provider?>(provider));

        await _handler.Handle(_event, _messageHandlerContextMock.Object);

        accountProviderLegalEntityRepository.Verify(a => 
            a.AddAccountProviderLegalEntity(
                It.IsAny<AccountProviderLegalEntity>(), 
                It.IsAny<CancellationToken>()
            ), 
            Times.Never
        );
        
        jobAuditRepository.Verify(m =>
            m.CreateJobAudit(
                It.IsAny<JobAudit>(), 
                It.IsAny<CancellationToken>()
            ), 
            Times.Never
        );

        permissionAuditRepository.Verify(m => 
            m.CreatePermissionAudit(
                It.IsAny<PermissionsAudit>(), 
                It.IsAny<CancellationToken>()
            ), 
            Times.Never
        );
    }

    private LiveVacancyModel CreateLiveVacanyModel()
    {
        return new LiveVacancyModel
        {
            VacancyId = Guid.NewGuid(),
            AccountPublicHashedId = "apPublicHashedId",
            TrainingProvider = new TrainingProviderModel { Ukprn = 12345678 },
            AccountLegalEntityPublicHashedId = "aplePublicHashedId"
        };
    }
}
