using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;
public interface IAccountProviderRepository
{
    Task<AccountProvider?> GetAccountProvider(long accountId, long providerUkprn);
}

public class AccountProviderRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IAccountProviderRepository
{
    public async Task<AccountProvider?> GetAccountProvider(long accountId, long providerUkprn)
    {
        return await _providerRelationshipsDataContext.AccountProviders
            .Where(c => c.AccountId == accountId && c.ProviderUkprn == providerUkprn).Select(c => c).FirstOrDefaultAsync();
    }
}
