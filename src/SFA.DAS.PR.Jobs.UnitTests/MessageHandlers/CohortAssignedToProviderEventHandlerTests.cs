using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework.Internal;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.MessageHandlers;
using SFA.DAS.PR.Jobs.OuterApi.Responses;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;
using System.Text.Json;

namespace SFA.DAS.PR.Jobs.UnitTests.MessageHandlers;

public sealed class CohortAssignedToProviderEventHandlerTests
{
    private CohortAssignedToProviderEventHandler _handler;
    private Mock<ILogger<CohortAssignedToProviderEventHandler>> _loggerMock;
    private Mock<IAccountProviderLegalEntityRepository> _accountProviderLegalEntityRepositoryMock;
    private Mock<ICommitmentsV2ApiClient> _commitmentsV2ApiMock;
    private Mock<IProviderRelationshipsDataContext> _providerRelationshipsDataContextMock;
    private Mock<IAccountLegalEntityRepository> _accountLegalEntityRepositoryMock;
    private Mock<IAccountProviderRepository> _accountProviderRepositoryMock;
    private CohortAssignedToProviderEvent _event;
    private Mock<IMessageHandlerContext> _messageHandlerContextMock;
    private Mock<IPermissionAuditRepository> _permissionAuditRepository;
    private Mock<IProvidersRepository> _providersRepository;
    private Mock<IJobAuditRepository> _jobAuditRepository;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<CohortAssignedToProviderEventHandler>>();
        _accountProviderLegalEntityRepositoryMock = new Mock<IAccountProviderLegalEntityRepository>();
        _providerRelationshipsDataContextMock = new Mock<IProviderRelationshipsDataContext>();
        _accountLegalEntityRepositoryMock = new Mock<IAccountLegalEntityRepository>();
        _accountProviderRepositoryMock = new Mock<IAccountProviderRepository>();
        _messageHandlerContextMock = new Mock<IMessageHandlerContext>();
        _permissionAuditRepository = new Mock<IPermissionAuditRepository>();
        _providersRepository = new Mock<IProvidersRepository>();
        _commitmentsV2ApiMock = new Mock<ICommitmentsV2ApiClient>();
        _jobAuditRepository = new Mock<IJobAuditRepository>();

        _handler = new CohortAssignedToProviderEventHandler(
            _loggerMock.Object,
            _commitmentsV2ApiMock.Object,
            _providerRelationshipsDataContextMock.Object,
            _accountLegalEntityRepositoryMock.Object,
            _providersRepository.Object,
            _accountProviderRepositoryMock.Object,
            _accountProviderLegalEntityRepositoryMock.Object,
            _permissionAuditRepository.Object,
            _jobAuditRepository.Object
        );

        _event = new CohortAssignedToProviderEvent(1, DateTime.UtcNow);
    }

    [Test]
    public async Task Handle_GetAccountLegalEntity_DoesNotExist_Returns()
    {
        AccountLegalEntity? response = null;

        _accountLegalEntityRepositoryMock.Setup(a => 
            a.GetAccountLegalEntity(
                It.IsAny<long>(), 
                It.IsAny<CancellationToken>()
            )
        ).ReturnsAsync(response);

        _commitmentsV2ApiMock.Setup(a =>
            a.GetCohortDetails(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()
            )
        ).ReturnsAsync(
            new CohortModel() 
            { 
                CohortId = 1, 
                AccountId = 1, 
                AccountLegalEntityId = 1,
                LegalEntityName = "LegalEntityName",
                ProviderName = "ProviderName",
                ProviderId = 1
            }
        );

        await _handler.Handle(_event, _messageHandlerContextMock.Object);

        _accountLegalEntityRepositoryMock.Verify(x =>
            x.GetAccountLegalEntity(
                It.IsAny<long>(), 
                It.IsAny<CancellationToken>()
            ), Times.Once
        );

        _providersRepository.Verify(x =>
            x.GetProvider(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()
            ), Times.Never
        );
    }

    [Test]
    public async Task Handle_GetProvider_DoesNotExist_Returns()
    {
        AccountLegalEntity response = AccountLegalEntityData.Create(1, 1);

        _accountLegalEntityRepositoryMock.Setup(a =>
            a.GetAccountLegalEntity(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()
            )
        ).ReturnsAsync(response);

        _commitmentsV2ApiMock.Setup(a =>
            a.GetCohortDetails(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()
            )
        ).ReturnsAsync(
            new CohortModel()
            {
                CohortId = 1,
                AccountId = 1,
                AccountLegalEntityId = 1,
                LegalEntityName = "LegalEntityName",
                ProviderName = "ProviderName",
                ProviderId = 1
            }
        );

        await _handler.Handle(_event, _messageHandlerContextMock.Object);

        _providersRepository.Verify(x =>
            x.GetProvider(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()
            ), Times.Once
        );

        _accountProviderRepositoryMock.Verify(x =>
            x.GetAccountProvider(
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()), 
            Times.Never
        );
    }

    [Test]
    public async Task Handle_CohortAssignedToProviderEvent_ProcessesEventAndCreatesAccountProviderLegalEntity()
    {
        Account account = AccountData.Create(1);

        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(account.Id, 1);

        Provider provider = new Provider() 
        { 
            Created = DateTime.UtcNow, 
            Name = "Name", 
            Ukprn = 10000001
        };

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
                .AddAccountLegalEntity(accountLegalEntity)
                .AddProvider(provider)
                .PersistChanges();

        PermissionAuditRepository permissionAuditRepository = new PermissionAuditRepository(context);

        AccountProviderRepository accountProviderRepository = new AccountProviderRepository(context);

        JobAuditRepository jobAuditRepository = new JobAuditRepository(context);

        AccountProviderLegalEntityRepository accountProviderLegalEntityRepository = new AccountProviderLegalEntityRepository(context);

        AccountLegalEntityRepository accountLegalEntityRepository = new AccountLegalEntityRepository(context);

        CohortAssignedToProviderEventHandler _handler = new CohortAssignedToProviderEventHandler(
            _loggerMock.Object,
            _commitmentsV2ApiMock.Object,
            context,
            accountLegalEntityRepository,
            _providersRepository.Object,
            _accountProviderRepositoryMock.Object,
            accountProviderLegalEntityRepository,
            permissionAuditRepository,
            jobAuditRepository
        );

        var response = new CohortModel()
        {
            CohortId = 1,
            AccountId = account.Id,
            AccountLegalEntityId = accountLegalEntity.Id,
            LegalEntityName = "LegalEntityName",
            ProviderName = "ProviderName",
            ProviderId = 1
        };

        _commitmentsV2ApiMock
            .Setup(x => x.GetCohortDetails(_event.CohortId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        _providersRepository
            .Setup(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((long ukprn, CancellationToken token) => new ValueTask<Provider?>(provider));

        await _handler.Handle(_event, _messageHandlerContextMock.Object);

        var sut = context.AccountProviderLegalEntities.First();
        var permissionAudit = context.PermissionsAudit.First();
        var notification = context.Notifications.First();
        var jobAudit = context.JobAudits.First();
        var accoountProviderLegalEntity = context.AccountProviderLegalEntities.First();

        Assert.Multiple(() =>
        {
            Assert.That(sut, Is.Not.Null);

            Assert.That(permissionAudit, Is.Not.Null);
            Assert.That(permissionAudit.Action, Is.EqualTo(nameof(PermissionAction.ApprovalsRelationship)));
            Assert.That(permissionAudit.Ukprn, Is.EqualTo(provider.Ukprn));
            Assert.That(permissionAudit.Operations, Is.EqualTo("[]"));

            Assert.That(notification, Is.Not.Null);
            Assert.That(notification.Ukprn, Is.EqualTo(provider.Ukprn));
            Assert.That(notification.CreatedBy, Is.EqualTo("PR Jobs: CohortAssignedToProviderEvent"));
            Assert.That(notification.TemplateName, Is.EqualTo("LinkedAccountCohort"));
            Assert.That(notification.NotificationType, Is.EqualTo(nameof(NotificationType.Provider)));

            Assert.That(accoountProviderLegalEntity, Is.Not.Null);

            Assert.That(jobAudit, Is.Not.Null);
            Assert.That(jobAudit.JobName, Is.EqualTo(nameof(CohortAssignedToProviderEvent)));
            Assert.That(jobAudit.JobInfo, Is.EqualTo($"{JsonSerializer.Serialize(_event)}"));
        });
    }
}