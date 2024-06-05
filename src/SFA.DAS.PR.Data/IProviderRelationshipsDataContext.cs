using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data;

public interface IProviderRelationshipsDataContext
{
    DbSet<AccountLegalEntity> AccountLegalEntities { get; }
    DbSet<AccountProviderLegalEntity> AccountProviderLegalEntities { get; }
    DbSet<AccountProvider> AccountProviders { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<Provider> Providers { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
