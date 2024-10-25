using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface IAccountLegalEntityRepository
{
    Task<AccountLegalEntity?> GetAccountLegalEntity(long id, CancellationToken cancellationToken);
}

public class AccountLegalEntityRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IAccountLegalEntityRepository
{
    public async Task<AccountLegalEntity?> GetAccountLegalEntity(long id, CancellationToken cancellationToken)
    {
        return await _providerRelationshipsDataContext.AccountLegalEntities
            .Include(a => a.Account)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}
