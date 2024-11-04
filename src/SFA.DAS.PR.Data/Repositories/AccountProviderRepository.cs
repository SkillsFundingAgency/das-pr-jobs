using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface IAccountProviderRepository
{
    ValueTask<AccountProvider?> GetAccountProvider(long providerUkprn, long accountId, CancellationToken cancellationToken);
    Task<AccountProvider?> AddAccountProvider(AccountProvider accountProvider, CancellationToken cancellationToken);
}

public class AccountProviderRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IAccountProviderRepository
{
    public async ValueTask<AccountProvider?> GetAccountProvider(long providerUkprn, long accountId, CancellationToken cancellationToken)
    {
        return await _providerRelationshipsDataContext.AccountProviders
            .AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.ProviderUkprn == providerUkprn &&
                a.AccountId == accountId,
                cancellationToken
        );
    }

    public async Task<AccountProvider?> AddAccountProvider(AccountProvider accountProvider, CancellationToken cancellationToken)
    {
        await _providerRelationshipsDataContext.AccountProviders.AddAsync(accountProvider, cancellationToken);
        return accountProvider;
    }
}
