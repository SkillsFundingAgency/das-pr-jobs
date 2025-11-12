using Refit;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.Infrastructure;

public interface IRoatpServiceApiClient
{
    [Get("/organisations")]
    Task<RegisteredProviderResponse> GetProviders(CancellationToken cancellationToken);
}
