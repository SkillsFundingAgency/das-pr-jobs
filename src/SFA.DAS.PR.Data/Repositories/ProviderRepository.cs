using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface IProviderRepository
{
    ValueTask<Provider?> GetProvider(long ukprn, CancellationToken cancellationToken);
}

public sealed class ProviderRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IProviderRepository
{
    public async ValueTask<Provider?> GetProvider(long ukprn, CancellationToken cancellationToken)
    {
        return await _providerRelationshipsDataContext.Providers.AsNoTracking().FirstOrDefaultAsync(a => a.Ukprn == ukprn, cancellationToken);
    }
}
