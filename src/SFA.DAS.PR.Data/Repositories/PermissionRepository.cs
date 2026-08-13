using Microsoft.EntityFrameworkCore;

namespace SFA.DAS.PR.Data.Repositories;

public interface IPermissionRepository
{
    Task<List<long>> GetAccountLegalEntityIdsWithPermissionsByProviderUkprn(int providerUkprn, CancellationToken cancellationToken);
}

public class PermissionRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IPermissionRepository
{
    public Task<List<long>> GetAccountLegalEntityIdsWithPermissionsByProviderUkprn(int providerUkprn, CancellationToken cancellationToken)
    {
        return _providerRelationshipsDataContext.Permissions
            .AsNoTracking()
            .Where(p => p.AccountProviderLegalEntity.AccountProvider.ProviderUkprn == providerUkprn)
            .Select(p => p.AccountProviderLegalEntity.AccountLegalEntityId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
