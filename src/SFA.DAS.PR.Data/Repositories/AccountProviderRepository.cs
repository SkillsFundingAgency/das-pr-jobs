using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;
public interface IAccountProviderRepository
{
    Task<AccountProvider?> GetAccountProvider(long accountId, long providerUkprn, CancellationToken cancellationToken);
    Task<AccountProvider?> CreateAccountProvider(AccountProvider accountProvider, CancellationToken cancellationToken);
}

public class AccountProviderRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IAccountProviderRepository
{
    public async Task<AccountProvider?> GetAccountProvider(long accountId, long providerUkprn, CancellationToken cancellationToken)
    {
        return await _providerRelationshipsDataContext.AccountProviders
            .FirstOrDefaultAsync(c => 
                c.AccountId == accountId && 
                c.ProviderUkprn == providerUkprn,
                cancellationToken
        );
    }

    public async Task<AccountProvider?> CreateAccountProvider(AccountProvider accountProvider, CancellationToken cancellationToken)
    {
        await _providerRelationshipsDataContext.AccountProviders.AddAsync(accountProvider, cancellationToken);
        return accountProvider;
    }
}
