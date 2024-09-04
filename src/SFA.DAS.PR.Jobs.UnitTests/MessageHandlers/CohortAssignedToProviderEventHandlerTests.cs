using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.MessageHandlers;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Jobs.UnitTests.MessageHandlers;
public class CohortAssignedToProviderEventHandlerTests
{
    private Mock<ILogger<CohortAssignedToProviderEventHandler>> _logger = new Mock<ILogger<CohortAssignedToProviderEventHandler>>();
    private Mock<ICommitmentsV2ApiClient> _commitmentsV2ApiClient = new Mock<ICommitmentsV2ApiClient>();
    private Mock<IProviderRelationshipsDataContext> _providerRelationshipsDataContext = new Mock<IProviderRelationshipsDataContext>();
    private Mock<IAccountLegalEntityRepository> _accountLegalEntityRepository = new Mock<IAccountLegalEntityRepository>();
    private Mock<IProvidersRepository> _providersRepository = new Mock<IProvidersRepository>();
    private Mock<IAccountProviderRepository> _accountProviderRepository = new Mock<IAccountProviderRepository>();
    private Mock<IAccountRepository> _accountRepository = new Mock<IAccountRepository>();
    private Mock<IAccountProviderLegalEntityRepository> _accountProviderLegalEntityRepository = new Mock<IAccountProviderLegalEntityRepository>();
    private readonly long _cohortId = 12345678;
    private CohortAssignedToProviderEvent _message;
    private Mock<IMessageHandlerContext> _messageContext = new();
    private Cohort _cohort;

    [SetUp]
    public void Setup()
    {
        _cohort = new Cohort
        {
            CohortId = _cohortId,
            AccountId = 1234567,
            AccountLegalEntityId = 123456,
            LegalEntityName = "TestLegalEntity",
            ProviderId = 12345,
            ProviderName = "TestProviderName"
        };

        _message = new CohortAssignedToProviderEvent(_cohortId, DateTime.UtcNow);

        _messageContext.Setup(c => c.MessageId).Returns("messageId");
    }

    [Test]
    public async Task CohortAssignedToProviderFunction_Run_NoAccountLegalEntityFound_NoFurtherAction()
    {
        _commitmentsV2ApiClient.Setup(x => x.GetCohortDetails(_cohortId, CancellationToken.None)).ReturnsAsync(_cohort);

        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(1, 1);

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccountLegalEntity(accountLegalEntity)
            .PersistChanges();

        _accountLegalEntityRepository = new Mock<IAccountLegalEntityRepository>();
        _providersRepository = new Mock<IProvidersRepository>();

        var sut = new CohortAssignedToProviderEventHandler(_logger.Object, _commitmentsV2ApiClient.Object,
            context, _accountLegalEntityRepository.Object, _providersRepository.Object,
            _accountProviderRepository.Object, _accountRepository.Object, _accountProviderLegalEntityRepository.Object);

        _accountLegalEntityRepository
            .Setup(x => x.GetAccountLegalEntity(_cohort.AccountLegalEntityId, CancellationToken.None))
            .ReturnsAsync((AccountLegalEntity?)null);

        await sut.Handle(_message, _messageContext.Object);

        using (new AssertionScope())
        {
            _accountLegalEntityRepository.Verify(
                x => x.GetAccountLegalEntity(_cohort.AccountLegalEntityId, CancellationToken.None),
                Times.Once);
            _providersRepository.Verify(x => x.GetProvider(It.IsAny<long>(), CancellationToken.None), Times.Never);
        }
    }

    [Test]
    public async Task CohortAssignedToProviderFunction_Run_NoProviderFound_NoFurtherAction()
    {
        _commitmentsV2ApiClient.Setup(x => x.GetCohortDetails(_cohortId, CancellationToken.None)).ReturnsAsync(_cohort);

        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(2, 2);
        Provider provider = ProvidersData.Create();

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccountLegalEntity(accountLegalEntity)
            .AddProvider(provider)
            .PersistChanges();

        _providersRepository = new Mock<IProvidersRepository>();
        _accountProviderRepository = new Mock<IAccountProviderRepository>();

        var sut = new CohortAssignedToProviderEventHandler(_logger.Object, _commitmentsV2ApiClient.Object,
            context, _accountLegalEntityRepository.Object, _providersRepository.Object,
            _accountProviderRepository.Object, _accountRepository.Object, _accountProviderLegalEntityRepository.Object);

        _accountLegalEntityRepository
            .Setup(x => x.GetAccountLegalEntity(_cohort.AccountLegalEntityId, CancellationToken.None))
            .ReturnsAsync(accountLegalEntity);

        _providersRepository.Setup(x => x.GetProvider(_cohort.ProviderId, CancellationToken.None))
            .ReturnsAsync((Provider?)null);

        await sut.Handle(_message, _messageContext.Object);

        using (new AssertionScope())
        {
            _providersRepository.Verify(x => x.GetProvider(_cohort.ProviderId, CancellationToken.None), Times.Once);
            _accountProviderRepository.Verify(x => x.GetAccountProvider(It.IsAny<long>(), It.IsAny<long>()),
                Times.Never);
        }
    }

    [Test]
    public async Task CohortAssignedToProviderFunction_Run_NoAccountProviderFound_AccountProviderIsCreated()
    {

        _commitmentsV2ApiClient.Setup(x => x.GetCohortDetails(_cohortId, CancellationToken.None)).ReturnsAsync(_cohort);

        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(3, 3);
        Provider provider = ProvidersData.Create();
        Account account = AccountData.Create(_cohort.AccountId);

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccountLegalEntity(accountLegalEntity)
            .AddProvider(provider)
            .AddAccount(account)
            .PersistChanges();

        var sut = new CohortAssignedToProviderEventHandler(_logger.Object, _commitmentsV2ApiClient.Object,
            context, _accountLegalEntityRepository.Object, _providersRepository.Object,
            _accountProviderRepository.Object, _accountRepository.Object, _accountProviderLegalEntityRepository.Object);

        _accountLegalEntityRepository
            .Setup(x => x.GetAccountLegalEntity(_cohort.AccountLegalEntityId, CancellationToken.None))
            .ReturnsAsync(accountLegalEntity);

        _providersRepository.Setup(x => x.GetProvider(_cohort.ProviderId, CancellationToken.None))
            .ReturnsAsync(provider);

        _accountProviderRepository.Setup(x => x.GetAccountProvider(_cohort.AccountId, _cohort.ProviderId))
            .ReturnsAsync((AccountProvider?)null);

        _accountRepository.Setup(x => x.GetAccount(_cohort.AccountId, CancellationToken.None)).ReturnsAsync(account);

        await sut.Handle(_message, _messageContext.Object);

        using (new AssertionScope())
        {
            context.AccountProviders.Count().Should().Be(1);
        }
    }

    [Test]
    public async Task CohortAssignedToProviderFunction_Run_AccountProviderLegalEntityExists_NoFurtherAction()
    {
        _commitmentsV2ApiClient.Setup(x => x.GetCohortDetails(_cohortId, CancellationToken.None)).ReturnsAsync(_cohort);

        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(4, 4);
        Provider provider = ProvidersData.Create();
        AccountProvider accountProvider = AccountProviderData.Create(_cohort.AccountId);
        AccountProviderLegalEntity accountProviderLegalEntity =
            AccountProviderLegalEntityData.Create(_cohort.AccountId, _cohort.AccountLegalEntityId);
        accountProvider.AccountProviderLegalEntities = new List<AccountProviderLegalEntity>
            { accountProviderLegalEntity };

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccountLegalEntity(accountLegalEntity)
            .AddProvider(provider)
            .AddAccountProvider(accountProvider)
            .AddAccountProviderLegalEntity(accountProviderLegalEntity)
            .PersistChanges();

        var sut = new CohortAssignedToProviderEventHandler(_logger.Object, _commitmentsV2ApiClient.Object,
            context, _accountLegalEntityRepository.Object, _providersRepository.Object,
            _accountProviderRepository.Object, _accountRepository.Object, _accountProviderLegalEntityRepository.Object);

        _accountLegalEntityRepository
            .Setup(x => x.GetAccountLegalEntity(_cohort.AccountLegalEntityId, CancellationToken.None))
            .ReturnsAsync(accountLegalEntity);

        _providersRepository.Setup(x => x.GetProvider(_cohort.ProviderId, CancellationToken.None))
            .ReturnsAsync(provider);

        _accountProviderRepository.Setup(x => x.GetAccountProvider(_cohort.AccountId, _cohort.ProviderId))
            .ReturnsAsync(accountProvider);

        _accountProviderLegalEntityRepository
            .Setup(x => x.GetAccountProviderLegalEntity(accountProvider.Id, _cohort.AccountLegalEntityId,
                CancellationToken.None)).ReturnsAsync(accountProviderLegalEntity);

        await sut.Handle(_message, _messageContext.Object);

        using (new AssertionScope())
        {
            _accountProviderLegalEntityRepository.Verify(
                x => x.GetAccountProviderLegalEntity(accountProvider.Id, _cohort.AccountLegalEntityId,
                    CancellationToken.None), Times.Once);
            context.AccountProviderLegalEntities.Count().Should().Be(1);
            context.JobAudits.Count().Should().Be(0);
            context.Notifications.Count().Should().Be(0);
        }
    }

    [Test]
    public async Task
        CohortAssignedToProviderFunction_Run_NoAccountProviderLegalEntityFound_AccountProviderLegalEntityCreated_AuditCreated_NotificationCreated()
    {
        _commitmentsV2ApiClient.Setup(x => x.GetCohortDetails(_cohortId, CancellationToken.None)).ReturnsAsync(_cohort);

        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(5, 5);
        Provider provider = ProvidersData.Create();
        AccountProvider accountProvider = AccountProviderData.Create(_cohort.AccountId);
        AccountProviderLegalEntity accountProviderLegalEntity =
            AccountProviderLegalEntityData.Create(_cohort.AccountId, _cohort.AccountLegalEntityId);
        accountProvider.AccountProviderLegalEntities = new List<AccountProviderLegalEntity>();

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccountLegalEntity(accountLegalEntity)
            .AddProvider(provider)
            .AddAccountProvider(accountProvider)
            .PersistChanges();

        var sut = new CohortAssignedToProviderEventHandler(_logger.Object, _commitmentsV2ApiClient.Object,
            context, _accountLegalEntityRepository.Object, _providersRepository.Object,
            _accountProviderRepository.Object, _accountRepository.Object, _accountProviderLegalEntityRepository.Object);

        _accountLegalEntityRepository
            .Setup(x => x.GetAccountLegalEntity(_cohort.AccountLegalEntityId, CancellationToken.None))
            .ReturnsAsync(accountLegalEntity);

        _providersRepository.Setup(x => x.GetProvider(_cohort.ProviderId, CancellationToken.None))
            .ReturnsAsync(provider);

        _accountProviderRepository.Setup(x => x.GetAccountProvider(_cohort.AccountId, _cohort.ProviderId))
            .ReturnsAsync(accountProvider);

        _accountProviderLegalEntityRepository
            .Setup(x => x.GetAccountProviderLegalEntity(accountProvider.Id, _cohort.AccountLegalEntityId,
                CancellationToken.None)).ReturnsAsync((AccountProviderLegalEntity?)null);

        await sut.Handle(_message, _messageContext.Object);

        using (new AssertionScope())
        {
            _accountProviderLegalEntityRepository.Verify(
                x => x.GetAccountProviderLegalEntity(accountProvider.Id, _cohort.AccountLegalEntityId,
                    CancellationToken.None), Times.Once);
            context.AccountProviderLegalEntities.Count().Should().Be(1);
            context.JobAudits.Count().Should().Be(1);
            context.Notifications.Count().Should().Be(1);
        }
    }
}
