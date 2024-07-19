using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;

namespace SFA.DAS.PR.Jobs.Services;

public interface ITokenService
{
    Task<Dictionary<string, string>> GetEmailTokens(Notification notification, CancellationToken cancellationToken);
}

public class TokenService(IProvidersRepository _providersRepository, IAccountLegalEntityRepository _accountLegalEntityRepository) : ITokenService
{
    public async Task<Dictionary<string, string>> GetEmailTokens(Notification notification, CancellationToken cancellationToken)
    {
        Dictionary<string, string> emailTokens = new();

        switch (Enum.Parse(typeof(NotificationType), notification.NotificationType))
        {
            case NotificationType.Provider:
                {
                    Provider? provider = await _providersRepository.GetProvider(notification.Ukprn!.Value, cancellationToken);

                    AccountLegalEntity? accountLegalEntity = await _accountLegalEntityRepository.GetAccountLegalEntity(notification.AccountLegalEntityId!.Value, cancellationToken);

                    emailTokens = new()
                    {
                        { EmailTokens.ProviderNameToken, provider!.Name },
                        { EmailTokens.EmployerNameToken, accountLegalEntity!.Name },
                        { EmailTokens.PermitRecruitToken, notification.PermitRecruit.ToString()! },
                        { EmailTokens.PermitApprovalsToken, notification.PermitApprovals.ToString()! }
                    };
                }
                break;
        }

        return emailTokens;
    }
}
