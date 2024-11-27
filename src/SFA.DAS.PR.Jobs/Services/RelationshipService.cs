using Microsoft.Extensions.Logging;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;

namespace SFA.DAS.PR.Jobs.Services;

public interface IRelationshipService
{
    Task<bool> CreateRelationship(RelationshipModel relationshipModel, CancellationToken cancellationToken);
}

public record struct RelationshipModel(
    long? AccountLegalEntityId,
    string? AccountLegalEntityPublicHashedId,
    long ProviderUkprn,
    string? AccountPublicHashId,
    string NotificationTemplateName,
    string permissionAuditAction
);

public sealed class RelationshipService(
    ILogger<RelationshipService> _logger,
    IAccountLegalEntityRepository _accountLegalEntityRepository,
    IProvidersRepository _providersRepository,
    IAccountProviderRepository _accountProviderRepository,
    IProviderRelationshipsDataContext _providerRelationshipsDataContext,
    IAccountProviderLegalEntityRepository _accountProviderLegalEntityRepository,
    IPermissionAuditRepository _permissionAuditRepository
) : IRelationshipService
{
    private const string SystemUser = "System";

    public async Task<bool> CreateRelationship(RelationshipModel relationshipModel, CancellationToken cancellationToken)
    {
        AccountLegalEntity? accountLegalEntity = await GetAccountLegalEntity(relationshipModel, cancellationToken);

        if (accountLegalEntity is null)
        {
            _logger.LogInformation(
                "Account Legal Entity for value {AccountLegalEntityValue} does not exist.",
                relationshipModel.AccountLegalEntityId.HasValue ?
                    relationshipModel.AccountLegalEntityId :
                    relationshipModel.AccountLegalEntityPublicHashedId
            );

            return false;
        }

        Provider? provider = await _providersRepository.GetProvider(relationshipModel.ProviderUkprn, cancellationToken);

        if (provider is null)
        {
            _logger.LogInformation("Provider for ukprn {Ukprn} does not exist.", relationshipModel.ProviderUkprn);
            return false;
        }

        AccountProvider? accountProvider = await _accountProviderRepository.GetAccountProvider(
            relationshipModel.ProviderUkprn,
            accountLegalEntity.AccountId,
            cancellationToken
        );

        bool accountProviderIsNull = false;
        if (accountProvider is null)
        {
            accountProviderIsNull = true;
            accountProvider = CreateAccountProvider(accountLegalEntity.AccountId, relationshipModel.ProviderUkprn);
        }

        AccountProviderLegalEntity? accountProviderLegalEntity = accountProviderIsNull ? null : await _accountProviderLegalEntityRepository.GetAccountProviderLegalEntity(
            accountProvider.Id,
            accountLegalEntity.Id,
            cancellationToken
        );

        if (accountProviderLegalEntity is not null)
        {
            _logger.LogInformation(
                "Account provider legal entity already exists: AccountProviderLegalEntityId: {AccountProviderLegalEntityId}.",
                accountProviderLegalEntity.Id
            );

            return false;
        }

        CreateAccountProviderLegalEntity(accountProvider, accountLegalEntity.Id);

        CreateNotification(provider.Ukprn, accountLegalEntity.Id, relationshipModel.NotificationTemplateName);

        await CreatePermissionAudit(accountLegalEntity, provider, relationshipModel, cancellationToken);

        return true;
    }

    private async Task<AccountLegalEntity?> GetAccountLegalEntity(RelationshipModel relationshipModel, CancellationToken cancellationToken)
    {
        if (!relationshipModel.AccountLegalEntityId.HasValue && string.IsNullOrWhiteSpace(relationshipModel.AccountLegalEntityPublicHashedId))
        {
            return null;
        }

        if (relationshipModel.AccountLegalEntityId.HasValue)
        {
            return await _accountLegalEntityRepository.GetAccountLegalEntity(
                relationshipModel.AccountLegalEntityId.Value,
                cancellationToken
            );
        }

        return await _accountLegalEntityRepository.GetAccountLegalEntity(
            relationshipModel.AccountLegalEntityPublicHashedId!,
            cancellationToken
        );
    }

    private void CreateNotification(long ukprn, long accountLegalEntityId, string notificationTemplateName)
    {
        _providerRelationshipsDataContext.Notifications.Add(new()
        {
            CreatedDate = DateTime.UtcNow,
            Ukprn = ukprn,
            AccountLegalEntityId = accountLegalEntityId,
            CreatedBy = SystemUser,
            NotificationType = nameof(NotificationType.Provider),
            TemplateName = notificationTemplateName
        });
    }

    private void CreateAccountProviderLegalEntity(AccountProvider accountProvider, long accountLegalEntityId)
    {
        AccountProviderLegalEntity accountProviderLegalEntity = new()
        {
            AccountProvider = accountProvider,
            AccountLegalEntityId = accountLegalEntityId,
            Created = DateTime.UtcNow
        };

        _accountProviderLegalEntityRepository.AddAccountProviderLegalEntity(accountProviderLegalEntity);
    }

    private async Task CreatePermissionAudit(AccountLegalEntity accountLegalEntity, Provider provider, RelationshipModel relationshipModel, CancellationToken cancellationToken)
    {
        await _permissionAuditRepository.CreatePermissionAudit(
            new()
            {
                Eventtime = DateTime.UtcNow,
                Action = relationshipModel.permissionAuditAction,
                Ukprn = provider.Ukprn,
                AccountLegalEntityId = accountLegalEntity.Id,
                Operations = "[]"
            },
            cancellationToken
        );
    }

    private AccountProvider CreateAccountProvider(long accountId, long providerId)
    {
        AccountProvider accountProvider = new()
        {
            AccountId = accountId,
            ProviderUkprn = providerId,
            Created = DateTime.UtcNow
        };

        _providerRelationshipsDataContext.AccountProviders.Add(accountProvider);

        return accountProvider;
    }
}
