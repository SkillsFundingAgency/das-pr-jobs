using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface IProvidersRepository
{
    Task<int> GetCount(CancellationToken cancellationToken);
    ValueTask<Provider?> GetProvider(long ukprn, CancellationToken cancellationToken);
}

public class ProvidersRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IProvidersRepository
{
    public async Task<int> GetCount(CancellationToken cancellationToken) => await _providerRelationshipsDataContext.Providers.CountAsync(cancellationToken);

    public async ValueTask<Provider?> GetProvider(long ukprn, CancellationToken cancellationToken)
    {
        return await _providerRelationshipsDataContext.Providers.FirstOrDefaultAsync(a => a.Ukprn == ukprn, cancellationToken);
    }
}
