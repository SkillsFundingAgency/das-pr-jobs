using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface IRequestsRepository
{
    ValueTask<Request?> GetRequest(Guid Id, CancellationToken cancellationToken);
}

public sealed class RequestsRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IRequestsRepository
{
    public async ValueTask<Request?> GetRequest(Guid Id, CancellationToken cancellationToken)
    {
        return await _providerRelationshipsDataContext.Requests.FirstOrDefaultAsync(a => a.Id == Id, cancellationToken);
    }
}
