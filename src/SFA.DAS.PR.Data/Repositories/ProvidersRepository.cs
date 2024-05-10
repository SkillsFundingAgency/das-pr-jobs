using Microsoft.EntityFrameworkCore;

namespace SFA.DAS.PR.Data.Repositories;

public class ProvidersRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IProvidersRepository
{
    public async Task<int> GetCount(CancellationToken cancellationToken) => await _providerRelationshipsDataContext.Providers.CountAsync(cancellationToken);
}
