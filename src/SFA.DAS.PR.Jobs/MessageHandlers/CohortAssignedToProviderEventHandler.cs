using Microsoft.Extensions.Logging;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.OuterApi.Responses;

namespace SFA.DAS.PR.Jobs.MessageHandlers;

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

        CohortModel cohort = await _commitmentsV2ApiClient.GetCohortDetails(message.CohortId, CancellationToken.None);

        AccountLegalEntity? accountLegalEntity = await _accountLegalEntityRepository.GetAccountLegalEntity(cohort.AccountLegalEntityId, context.CancellationToken);

        if (accountLegalEntity is null)
        {
            _logger.LogInformation("Account Legal Entity for id {AccountLegalEntityId} does not exist.", cohort.AccountLegalEntityId);
            return;
        }

        Provider? provider = await _providersRepository.GetProvider(cohort.ProviderId, context.CancellationToken);

        if (provider is null)
        {
            _logger.LogInformation("Provider for ukprn {Ukprn} does not exist.", cohort.ProviderId);
            return;
        }

        AccountProvider? accountProvider = await _accountProviderRepository.GetAccountProvider(cohort.ProviderId, cohort.AccountId, context.CancellationToken);

        if (accountProvider is null)
        {
            accountProvider = await CreateAccountProvider(cohort.AccountId, cohort.ProviderId, context.CancellationToken);
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

        await CreateAccountProviderLegalEntity(accountProvider, cohort.AccountLegalEntityId, context.CancellationToken);

        await CreatePermissionAudit(accountLegalEntity, provider, context.CancellationToken);

        await CreateNotification(provider.Ukprn, cohort.AccountLegalEntityId, context.CancellationToken);

        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "{EventHandlerName} completed at: {TimeStamp}. AccountProviderLegalEntity created successfully.",
            nameof(CohortAssignedToProviderEventHandler),
            DateTime.UtcNow
        );
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

        return accountProvider;
    }

    private async Task CreateAccountProviderLegalEntity(AccountProvider accountProvider, long accountLegalEntityId, CancellationToken cancellationToken)
    {
        AccountProviderLegalEntity accountProviderLegalEntity = new ()
        {
            AccountProvider = accountProvider,
            AccountLegalEntityId = accountLegalEntityId,
            Created = DateTime.UtcNow
        };

        await _providerRelationshipsDataContext.AccountProviderLegalEntities.AddAsync(accountProviderLegalEntity, cancellationToken);
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

    private async Task CreateNotification(long ukprn, long accountLegalEntityId, CancellationToken cancellationToken)
    {
        Notification notification = new ()
        {
            CreatedDate = DateTime.UtcNow,
            Ukprn = ukprn,
            AccountLegalEntityId = accountLegalEntityId,
            CreatedBy = "PR Jobs: CohortAssignedToProviderEvent",
            NotificationType = nameof(NotificationType.Provider),
            TemplateName = "LinkedAccountCohort"
        };
        await _providerRelationshipsDataContext.Notifications.AddAsync(notification, cancellationToken);
    }
}
