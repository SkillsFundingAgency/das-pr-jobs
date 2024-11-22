using Refit;
using SFA.DAS.PR.Jobs.OuterApi.Responses;

namespace SFA.DAS.PR.Jobs.Infrastructure;

public interface IEmployerAccountsApiClient
{
    [Get("/api/accounts/{accountId}")]
    Task<AccountDetails> GetAccount(long accountId, CancellationToken cancellationToken);
}
