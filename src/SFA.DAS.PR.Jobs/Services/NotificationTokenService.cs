using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;

namespace SFA.DAS.PR.Jobs.Services;

public interface INotificationTokenService
{
    Task<Dictionary<string, string>> GetEmailTokens(Notification notification, CancellationToken cancellationToken);
}

public class NotificationTokenService(IProvidersRepository _providersRepository, IAccountLegalEntityRepository _accountLegalEntityRepository) : INotificationTokenService
{
    private const string ApprovalsAdd = "add";
    private const string ApprovalsCannotAdd = "cannot add";
    private const string RecruitCreate = "create";
    private const string RecruitCreateAndPublish = "create and publish";
    private const string RecruitCannotCreate = "cannot create";

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
                        { EmailTokens.EmployerNameToken, accountLegalEntity!.Name }
                    };

                    if(notification.PermitRecruit.HasValue)
                    {
                        emailTokens.Add(EmailTokens.PermitRecruitToken, SetRecruitToken(notification.PermitRecruit)!);
                    }

                    if (notification.PermitApprovals.HasValue)
                    {
                        emailTokens.Add(EmailTokens.PermitApprovalsToken, SetApprovalsToken(notification.PermitApprovals)!);
                    }
                }
                break;
        }

        return emailTokens;
    }

    private static string? SetRecruitToken(short? permitRecruit)
    {
        return permitRecruit switch
        {
            0 => RecruitCannotCreate,
            1 => RecruitCreateAndPublish,
            2 => RecruitCreate,
            _ => null
        };
    }

    private static string? SetApprovalsToken(short? permitApprovals)
    {
        return permitApprovals switch
        {
            0 => ApprovalsCannotAdd,
            1 => ApprovalsAdd,
            _ => null
        };
    }
}

