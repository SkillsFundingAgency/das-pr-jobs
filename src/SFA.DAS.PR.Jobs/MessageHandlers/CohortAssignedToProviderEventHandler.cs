using Microsoft.Extensions.Logging;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Infrastructure;
using System.Diagnostics.CodeAnalysis;

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
    private readonly IAccountProviderLegalEntityRepository _accountProviderLegalEntityRepository;
    private readonly IPermissionAuditRepository _permissionAuditRepository;

    public CohortAssignedToProviderEventHandler(
        ILogger<CohortAssignedToProviderEventHandler> logger,
        ICommitmentsV2ApiClient commitmentsV2ApiClient,
        IProviderRelationshipsDataContext providerRelationshipsDataContext, 
        IAccountLegalEntityRepository accountLegalEntityRepository, 
        IProvidersRepository providersRepository, 
        IAccountProviderRepository accountProviderRepository, 
        IAccountProviderLegalEntityRepository accountProviderLegalEntityRepository,
        IPermissionAuditRepository permissionAuditRepository
    )
    {
        _logger = logger;
        _commitmentsV2ApiClient = commitmentsV2ApiClient;
        _providerRelationshipsDataContext = providerRelationshipsDataContext;
        _accountLegalEntityRepository = accountLegalEntityRepository;
        _providersRepository = providersRepository;
        _accountProviderRepository = accountProviderRepository;
        _accountProviderLegalEntityRepository = accountProviderLegalEntityRepository;
        _permissionAuditRepository = permissionAuditRepository;
    }

    public async Task Handle(CohortAssignedToProviderEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("CohortAssignedToProviderEventHandler started at: {TimeStamp}", DateTime.UtcNow);

        Cohort cohort = await _commitmentsV2ApiClient.GetCohortDetails(message.CohortId, CancellationToken.None);

        AccountLegalEntity? accountLegalEntity = await _accountLegalEntityRepository.GetAccountLegalEntity(cohort.AccountLegalEntityId, context.CancellationToken);

        if(accountLegalEntity is null)
        {
            return;
        }

        Provider? provider = await _providersRepository.GetProvider(cohort.ProviderId, context.CancellationToken);

        if (provider is null)
        {
            return;
        }

        AccountProvider? accountProvider = await GetAccountProvider(cohort.AccountId, cohort.ProviderId, context.CancellationToken);

        if(accountProvider is null)
        {
            return;
        }

        AccountProviderLegalEntity? accountProviderLegalEntity = await _accountProviderLegalEntityRepository.GetAccountProviderLegalEntity(
            accountProvider.Id,
            accountLegalEntity.Id,
            context.CancellationToken
        );

        if (accountProviderLegalEntity is not null)
        {
            return;
        }

        await CreateAccountProviderLegalEntity(accountProvider.Id, cohort.AccountLegalEntityId, context.CancellationToken);

        await CreatePermissionAudit(accountLegalEntity, provider, context.CancellationToken);

        await CreateNotification(provider.Ukprn, cohort.AccountLegalEntityId);

        _logger.LogInformation(
            "CohortAssignedToProviderEventHandler completed at: {TimeStamp}. AccountProviderLegalEntity created successfully.",
            DateTime.UtcNow
        );
    }

    private async Task<AccountProvider?> GetAccountProvider(long accountId, long providerId, CancellationToken cancellationToken)
    {
        AccountProvider? accountProvider = await _accountProviderRepository.GetAccountProvider(accountId, providerId, cancellationToken);

        if (accountProvider is not null)
        {
            return accountProvider;
        }

        return await CreateAccountProvider(accountId, providerId, cancellationToken);
    }

    private async Task<AccountProvider> CreateAccountProvider(long accountId, long providerId, CancellationToken cancellationToken)
    {
        AccountProvider accountProvider = new ()
        {
            AccountId = accountId,
            ProviderUkprn = providerId,
            Created = DateTime.UtcNow
        };

        await _providerRelationshipsDataContext.AccountProviders.AddAsync(accountProvider, cancellationToken);

        await _providerRelationshipsDataContext.SaveChangesAsync(cancellationToken);

        return accountProvider;
    }

    private async Task CreateAccountProviderLegalEntity(long accountProviderId, long accountLegalEntityId, CancellationToken cancellationToken)
    {
        AccountProviderLegalEntity accountProviderLegalEntity = new ()
        {
            AccountProviderId = accountProviderId,
            AccountLegalEntityId = accountLegalEntityId,
            Created = DateTime.UtcNow
        };

        await _providerRelationshipsDataContext.AccountProviderLegalEntities.AddAsync(accountProviderLegalEntity, cancellationToken);

        await _providerRelationshipsDataContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreatePermissionAudit(AccountLegalEntity accountLegalEntity, Provider provider, CancellationToken cancellationToken)
    {
        await _permissionAuditRepository.CreatePermissionAudit(
            new ()
            {
                Eventtime = DateTime.UtcNow,
                Action = nameof(PermissionAction.ApprovalsRelationship),
                Ukprn = provider.Ukprn,
                AccountLegalEntityId = accountLegalEntity.Id,
                Operations = "[]"
            },
            cancellationToken
        );
    }

    private async Task CreateNotification(long ukprn, long accountLegalEntityId)
    {
        Notification notification = new ()
        {
            Ukprn = ukprn,
            AccountLegalEntityId = accountLegalEntityId,
            CreatedBy = "PR Jobs: CohortAssignedToProviderEvent",
            NotificationType = nameof(NotificationType.Provider),
            TemplateName = "LinkedAccountCohort"
        };
        _providerRelationshipsDataContext.Notifications.Add(notification);

        await _providerRelationshipsDataContext.SaveChangesAsync(CancellationToken.None);
    }
}
