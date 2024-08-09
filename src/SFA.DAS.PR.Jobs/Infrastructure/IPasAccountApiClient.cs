using Refit;
using SFA.DAS.PAS.Account.Api.Types;

namespace SFA.DAS.PR.Jobs.Infrastructure;

public interface IPasAccountApiClient
{
    [Post("/api/email/{ukprn}/send")]
    Task SendEmailToAllProviderRecipients([AliasAs("ukprn")] long ukprn, [Body] ProviderEmailRequest message, CancellationToken cancellationToken);
}
