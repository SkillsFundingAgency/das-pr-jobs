using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface IAccountProviderLegalEntityRepository
{
    Task<AccountProviderLegalEntity?> GetAccountProviderLegalEntity(long accountProviderId, long accountLegalEntityId, CancellationToken cancellationToken);
    Task AddAccountProviderLegalEntity(AccountProviderLegalEntity accountProviderLegalEntity, CancellationToken cancellationToken);
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

    public async Task AddAccountProviderLegalEntity(AccountProviderLegalEntity accountProviderLegalEntity, CancellationToken cancellationToken)
    {
        await _providerRelationshipsDataContext.AccountProviderLegalEntities.AddAsync(accountProviderLegalEntity, cancellationToken);
    }
}
