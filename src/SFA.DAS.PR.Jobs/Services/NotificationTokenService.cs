using Microsoft.Extensions.Options;
using SFA.DAS.Encoding;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Configuration;

namespace SFA.DAS.PR.Jobs.Services;

public interface INotificationTokenService
{
    ValueTask<Dictionary<string, string>> GetEmailTokens(Notification notification, CancellationToken cancellationToken);
}

public class NotificationTokenService(
    IProvidersRepository _providersRepository,
    IAccountLegalEntityRepository _accountLegalEntityRepository,
    IRequestsRepository _requestsRepository,
    IOptions<NotificationsConfiguration> _notificationConfigurationOptions
) : INotificationTokenService
{
    public async ValueTask<Dictionary<string, string>> GetEmailTokens(Notification notification, CancellationToken cancellationToken)
    {
        Dictionary<string, string> emailTokens = new();

        Request? request = await GetRequest(notification.RequestId, cancellationToken);

        await AddProviderTokens(notification, request, emailTokens, cancellationToken);

        await AddAccountLegalEntityTokens(notification, request, emailTokens, cancellationToken);

        AddNotificationSpecificTokens(notification, request, emailTokens);

        return emailTokens;
    }

    private async Task AddProviderTokens(Notification notification, Request? request, Dictionary<string, string> emailTokens, CancellationToken cancellationToken)
    {
        Provider? provider = await GetProvider(notification, request, cancellationToken);

        if (provider is null)
            return;

        emailTokens.Add(NotificationTokens.ProviderName, provider.Name);
        emailTokens.Add(NotificationTokens.Ukprn, provider.Ukprn.ToString());
    }

    private async Task AddAccountLegalEntityTokens(Notification notification, Request? request, Dictionary<string, string> emailTokens, CancellationToken cancellationToken)
    {
        if (notification.AccountLegalEntityId.HasValue)
        {
            AccountLegalEntity? accountLegalEntity = await _accountLegalEntityRepository.GetAccountLegalEntity(notification.AccountLegalEntityId.Value, cancellationToken);

            if (accountLegalEntity is not null)
            {
                emailTokens.Add(NotificationTokens.EmployerName, accountLegalEntity.Name);
                emailTokens.Add(NotificationTokens.AccountLegalEntityHashedId, accountLegalEntity.PublicHashedId);
                emailTokens.Add(NotificationTokens.AccountHashedId, accountLegalEntity.Account.HashedId);
            }
        }
        else if (HasValidOrganisationName(request))
        {
            emailTokens.Add(NotificationTokens.EmployerName, request!.EmployerOrganisationName!);
        }
    }

    private void AddNotificationSpecificTokens(Notification notification, Request? request, Dictionary<string, string> emailTokens)
    {
        if (!Enum.TryParse(notification.NotificationType, out NotificationType notificationType))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(notification.EmailAddress))
        {
            emailTokens.Add(NotificationTokens.EmailAddress, notification.EmailAddress);
        }

        switch (notificationType)
        {
            case NotificationType.Provider:
                {
                    AddProviderSpecificTokens(notification, emailTokens);
                }
                break;
            case NotificationType.Employer:
                {
                    AddEmployerSpecificTokens(notification, request, emailTokens);
                }
                break;
        }
    }

    private void AddProviderSpecificTokens(Notification notification, Dictionary<string, string> emailTokens)
    {
        if (notification.PermitRecruit.HasValue)
        {
            emailTokens.Add(NotificationTokens.PermitRecruit, SetRecruitToken(notification.PermitRecruit)!);
        }

        if (notification.PermitApprovals.HasValue)
        {
            emailTokens.Add(NotificationTokens.PermitApprovals, SetApprovalsToken(notification.PermitApprovals)!);
        }

        emailTokens.Add(NotificationTokens.ProviderPortalUrl, _notificationConfigurationOptions.Value.ProviderPortalUrl);
        emailTokens.Add(NotificationTokens.ProviderPRWeb, _notificationConfigurationOptions.Value.ProviderPRBaseUrl);
    }

    private void AddEmployerSpecificTokens(Notification notification, Request? request, Dictionary<string, string> emailTokens)
    {
        if (!string.IsNullOrWhiteSpace(notification.Contact))
        {
            emailTokens.Add(NotificationTokens.Contact, notification.Contact);
        }

        if (request is not null)
        {
            emailTokens.Add(NotificationTokens.RequestId, request.Id.ToString());
        }

        emailTokens.Add(NotificationTokens.RequestExpiry, _notificationConfigurationOptions.Value.RequestExpiry.ToString());
        emailTokens.Add(NotificationTokens.EmployerPRWeb, _notificationConfigurationOptions.Value.EmployerPRBaseUrl.ToString());
        emailTokens.Add(NotificationTokens.EmployerAccountsWeb, _notificationConfigurationOptions.Value.EmployerAccountsBaseUrl);
    }

    private static bool HasValidOrganisationName(Request? request)
    {
        return request is not null && !string.IsNullOrWhiteSpace(request.EmployerOrganisationName);
    }

    private async ValueTask<Request?> GetRequest(Guid? requestId, CancellationToken cancellationToken)
    {
        return requestId.HasValue ? await _requestsRepository.GetRequest(requestId.Value, cancellationToken) : null;
    }

    private async ValueTask<Provider?> GetProvider(Notification notification, Request? request, CancellationToken cancellationToken)
    {
        if (notification.Ukprn.HasValue)
        {
            return await _providersRepository.GetProvider(notification.Ukprn.Value, cancellationToken);
        }

        return request?.Ukprn is not null ? await _providersRepository.GetProvider(request.Ukprn, cancellationToken) : null;
    }

    private static string? SetRecruitToken(short? permitRecruit)
    {
        return permitRecruit switch
        {
            0 => NotificationTokens.RecruitCannotCreate,
            1 => NotificationTokens.RecruitCreateAndPublish,
            2 => NotificationTokens.RecruitCreate,
            _ => null
        };
    }

    private static string? SetApprovalsToken(short? permitApprovals)
    {
        return permitApprovals switch
        {
            0 => NotificationTokens.ApprovalsCannotAdd,
            1 => NotificationTokens.ApprovalsAdd,
            _ => null
        };
    }
}
