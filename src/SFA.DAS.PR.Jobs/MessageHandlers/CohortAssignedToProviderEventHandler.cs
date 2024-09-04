using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Infrastructure;

namespace SFA.DAS.PR.Jobs.MessageHandlers;

[ExcludeFromCodeCoverage]
public class CohortAssignedToProviderEventHandler : IHandleMessages<CohortAssignedToProviderEvent>
{
    private readonly ILogger<CohortAssignedToProviderEventHandler> _logger;
    private readonly ICommitmentsV2ApiClient _commitmentsV2ApiClient;
    private readonly IProviderRelationshipsDataContext _providerRelationshipsDataContext;
    private readonly IAccountLegalEntityRepository _accountLegalEntityRepository;
    private readonly IProvidersRepository _providersRepository;
    private readonly IAccountProviderRepository _accountProviderRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountProviderLegalEntityRepository _accountProviderLegalEntityRepository;

    public CohortAssignedToProviderEventHandler(
        ILogger<CohortAssignedToProviderEventHandler> logger,
        ICommitmentsV2ApiClient commitmentsV2ApiClient,
        IProviderRelationshipsDataContext providerRelationshipsDataContext, IAccountLegalEntityRepository accountLegalEntityRepository, IProvidersRepository providersRepository, IAccountProviderRepository accountProviderRepository, IAccountRepository accountRepository, IAccountProviderLegalEntityRepository accountProviderLegalEntityRepository)
    {
        _logger = logger;
        _commitmentsV2ApiClient = commitmentsV2ApiClient;
        _providerRelationshipsDataContext = providerRelationshipsDataContext;
        _accountLegalEntityRepository = accountLegalEntityRepository;
        _providersRepository = providersRepository;
        _accountProviderRepository = accountProviderRepository;
        _accountRepository = accountRepository;
        _accountProviderLegalEntityRepository = accountProviderLegalEntityRepository;
    }

    [Function(nameof(CohortAssignedToProviderEventHandler))]
    public async Task Handle(CohortAssignedToProviderEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("CohortAssignedToProviderEventHandler started at: {TimeStamp}", DateTime.UtcNow);

        var cohort = await _commitmentsV2ApiClient.GetCohortDetails(message.CohortId, CancellationToken.None);

        if (!AccountLegalEntityIdExists(cohort.AccountLegalEntityId))
        {
            _logger.LogInformation(
                "CohortAssignedToProviderEventHandler completed at: {TimeStamp}. No AccountLegalEntity found for AccountLegalEntityId {AccountLegalEntityId}.",
                DateTime.UtcNow, cohort.AccountLegalEntityId);
        }
        else
        {

            if (!ProviderIdExists(cohort.ProviderId))
            {
                _logger.LogInformation(
                    "CohortAssignedToProviderEventHandler completed at: {TimeStamp}. No Provider found for ProviderId: {ProviderId}.",
                    DateTime.UtcNow, cohort.ProviderId);
            }
            else
            {
                var accountProviderId = GetAccountProviderId(cohort.AccountId, cohort.ProviderId);

                if (AccountProviderLegalEntitiesExists(accountProviderId, cohort.AccountLegalEntityId))
                {
                    _logger.LogInformation(
                    "CohortAssignedToProviderEventHandler completed at: {TimeStamp}. AccountProviderLegalEntity found successfully.",
                    DateTime.UtcNow);
                }
                else
                {
                    await CreateAccountProviderLegalEntityWithAudit(accountProviderId, cohort);

                    _logger.LogInformation(
                        "CohortAssignedToProviderEventHandler completed at: {TimeStamp}. AccountProviderLegalEntity created successfully.",
                        DateTime.UtcNow);
                }
            }
        }
    }

    private bool AccountLegalEntityIdExists(long accountLegalEntityId)
    {
        var accountLegalEntity = _accountLegalEntityRepository.GetAccountLegalEntity(accountLegalEntityId, CancellationToken.None).Result;
        return accountLegalEntity != null;
    }

    private bool ProviderIdExists(long providerId)
    {
        var provider = _providersRepository.GetProvider(providerId, CancellationToken.None).Result;
        return provider != null;
    }

    private long GetAccountProviderId(long accountId, long providerId)
    {
        AccountProvider? accountProvider = _accountProviderRepository.GetAccountProvider(accountId, providerId).Result;

        if (accountProvider == null)
        {
            var accountProviderId = CreateAccountProvider(accountId, providerId).Result;

            return accountProviderId;
        }

        return accountProvider.Id;
    }

    private async Task<long> CreateAccountProvider(long accountId, long providerId)
    {
        Account? account = _accountRepository.GetAccount(accountId, CancellationToken.None).Result;
        Provider? provider = _providersRepository.GetProvider(providerId, CancellationToken.None).Result;

        AccountProvider newAccountProvider = new AccountProvider
        {
            AccountId = accountId,
            ProviderUkprn = providerId,
            Account = account,
            Provider = provider,
            Created = DateTime.UtcNow
        };

        _providerRelationshipsDataContext.AccountProviders.Add(newAccountProvider);

        await _providerRelationshipsDataContext.SaveChangesAsync(CancellationToken.None);

        return newAccountProvider.Id;
    }

    private bool AccountProviderLegalEntitiesExists(long accountProviderId, long accountLegalEntityId)
    {
        var accountProviderLegalEntity =
            _accountProviderLegalEntityRepository.GetAccountProviderLegalEntity(accountProviderId, accountLegalEntityId,
                CancellationToken.None).Result;

        return accountProviderLegalEntity != null;
    }

    private async Task CreateAccountProviderLegalEntityWithAudit(long accountProviderId, Cohort cohort)
    {
        await CreateAccountProviderLegalEntity(accountProviderId, cohort.AccountLegalEntityId);

        await CreateAudit();

        await CreateNotification(cohort.ProviderId, cohort.AccountLegalEntityId);
    }

    private async Task CreateAccountProviderLegalEntity(long accountProviderId, long accountLegalEntityId)
    {
        AccountProviderLegalEntity accountProviderLegalEntity = new AccountProviderLegalEntity
        {
            AccountProviderId = accountProviderId,
            AccountLegalEntityId = accountLegalEntityId,
            Created = DateTime.UtcNow
        };

        _providerRelationshipsDataContext.AccountProviderLegalEntities.Add(accountProviderLegalEntity);

        await _providerRelationshipsDataContext.SaveChangesAsync(CancellationToken.None);
    }

    private async Task CreateAudit()
    {
        JobAudit jobAudit = new JobAudit
        {
            JobName = nameof(CohortAssignedToProviderEventHandler),
            JobInfo = ""
        };
        //Not sure what to put for JobInfo
        _providerRelationshipsDataContext.JobAudits.Add(jobAudit);

        await _providerRelationshipsDataContext.SaveChangesAsync(CancellationToken.None);
    }

    private async Task CreateNotification(long accountProviderId, long accountLegalEntityId)
    {
        Notification notification = new Notification
        {
            Ukprn = accountLegalEntityId,
            AccountLegalEntityId = accountLegalEntityId,
            CreatedBy = "PR Jobs: CohortAssignedToProviderEvent",
            NotificationType = "provider",
            TemplateName = "LinkedAccountCohort"
        };
        _providerRelationshipsDataContext.Notifications.Add(notification);

        await _providerRelationshipsDataContext.SaveChangesAsync(CancellationToken.None);
    }
}