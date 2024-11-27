using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface IAccountProviderLegalEntityRepository
{
    Task<AccountProviderLegalEntity?> GetAccountProviderLegalEntity(long accountProviderId, long accountLegalEntityId, CancellationToken cancellationToken);
    void AddAccountProviderLegalEntity(AccountProviderLegalEntity accountProviderLegalEntity);
}

public class AccountProviderLegalEntityRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IAccountProviderLegalEntityRepository
{
    public async Task<AccountProviderLegalEntity?> GetAccountProviderLegalEntity(long accountProviderId, long accountLegalEntityId, CancellationToken cancellationToken)
    {
        return await _providerRelationshipsDataContext.AccountProviderLegalEntities
            .AsNoTracking()
        .FirstOrDefaultAsync(a =>
            a.AccountProviderId == accountProviderId &&
            a.AccountLegalEntityId == accountLegalEntityId,
            cancellationToken
        );
    }

    public void AddAccountProviderLegalEntity(AccountProviderLegalEntity accountProviderLegalEntity)
    {
        _providerRelationshipsDataContext.AccountProviderLegalEntities.Add(accountProviderLegalEntity);
    }
}
