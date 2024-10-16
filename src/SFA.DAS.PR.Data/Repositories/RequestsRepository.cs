using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;
using System.Threading;

namespace SFA.DAS.PR.Data.Repositories;

public interface IRequestsRepository
{
    ValueTask<Request?> GetRequest(Guid Id, CancellationToken cancellationToken);
    ValueTask<IEnumerable<Request>> GetExpiredRequests(int expirationPeriod, CancellationToken cancellationToken);
}

public sealed class RequestsRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IRequestsRepository
{
    public async ValueTask<Request?> GetRequest(Guid Id, CancellationToken cancellationToken)
    {
        return await _providerRelationshipsDataContext.Requests.FirstOrDefaultAsync(a => a.Id == Id, cancellationToken);
    }

    public async ValueTask<IEnumerable<Request>> GetExpiredRequests(int expirationPeriod, CancellationToken cancellationToken)
    {
        var expiredOnDate = DateTime.UtcNow.AddDays(expirationPeriod * -1);

        var expiredRequests = await _providerRelationshipsDataContext.Requests
            .Where(a => a.RequestedDate < expiredOnDate)
            .ToListAsync(cancellationToken);

        return expiredRequests;
    }
}
