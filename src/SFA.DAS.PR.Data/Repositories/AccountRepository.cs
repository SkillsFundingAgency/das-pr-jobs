using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;
public interface IAccountRepository
{
    Task<Account?> GetAccount(long id, CancellationToken cancellationToken);
}

public class AccountRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IAccountRepository
{
    public async Task<Account?> GetAccount(long id, CancellationToken cancellationToken)
    {
        return await _providerRelationshipsDataContext.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}
