using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.Notifications.Messages.Commands;
using SFA.DAS.PAS.Account.Api.Types;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Configuration;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Services;

namespace SFA.DAS.PR.Jobs.Functions.Notifications;

public sealed class SendNotificationsFunction
{
    private readonly ILogger<SendNotificationsFunction> _logger;
    private readonly IProviderRelationshipsDataContext _providerRelationshipsDataContext;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationTokenService _notificationTokenService;
    private readonly IPasAccountApiClient _pasAccountApiClient;
    private readonly IFunctionEndpoint _functionEndpoint;
    private readonly IRequestsRepository _requestsRepository;
    private readonly NotificationsConfiguration _notificationsConfiguration;

    public SendNotificationsFunction(
        ILogger<SendNotificationsFunction> logger,
        IProviderRelationshipsDataContext providerRelationshipsDataContext,
        INotificationRepository notificationRepository,
        INotificationTokenService notificationTokenService,
        IPasAccountApiClient pasAccountApiClient,
        IFunctionEndpoint functionEndpoint,
        IRequestsRepository requestsRepository,
        IOptions<NotificationsConfiguration> notificationsConfigurationOptions
    )
    {
        _logger = logger;
        _providerRelationshipsDataContext = providerRelationshipsDataContext;
        _notificationRepository = notificationRepository;
        _notificationTokenService = notificationTokenService;
        _pasAccountApiClient = pasAccountApiClient;
        _notificationsConfiguration = notificationsConfigurationOptions.Value;
        _functionEndpoint = functionEndpoint;
        _requestsRepository = requestsRepository;
    }

    [Function(nameof(SendNotificationsFunction))]
    public async Task Run([TimerTrigger("%SendNotificationsFunctionSchedule%", RunOnStartup = false)] TimerInfo timer, FunctionContext executionContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{FunctionName} has been triggered.", nameof(SendNotificationsFunction));

        List<Notification> notifications = await _notificationRepository.GetPendingNotifications(
            _notificationsConfiguration.BatchSize,
            cancellationToken
        );

        int processedCount = 0;

        if (notifications.Any())
        {
            foreach (Notification notification in notifications)
            {
                processedCount += await TryProcessNotification(notification, executionContext, cancellationToken);
            }

            await _providerRelationshipsDataContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("{FunctionName} - Processed {ProcessedCount} notifications.", nameof(SendNotificationsFunction), processedCount);
    }

    private async Task<int> TryProcessNotification(Notification notification, FunctionContext executionContext, CancellationToken cancellationToken)
    {
        try
        {
            Request? request = notification.RequestId.HasValue ?
                await _requestsRepository.GetRequest(notification.RequestId.Value, cancellationToken) :
                null;

            switch (notification.NotificationType)
            {
                case nameof(NotificationType.Provider):
                    {
                        ProviderEmailRequest providerEmailRequest = await CreateProviderEmailRequest(notification, cancellationToken);

                        long? ukprn = GetProviderUkprn(notification, request);

                        await _pasAccountApiClient.SendEmailToAllProviderRecipients(
                            ukprn!.Value, 
                            providerEmailRequest, 
                            cancellationToken
                        );
                    }
                    break;
                case nameof(NotificationType.Employer):
                    {
                        SendEmailCommand employerEmailRequest = await CreateEmployerEmailRequest(notification, cancellationToken);

                        await _functionEndpoint.Send(employerEmailRequest, executionContext, cancellationToken);
                    }
                    break;
            }

            UpdateNotification(notification, request);

            if (request is not null)
            {
                request.Status = RequestStatus.Sent;
            }

            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending out notification with id: {NotificationId}", notification.Id);

            return 0;
        }
    }

    private static long? GetProviderUkprn(Notification notification, Request? request)
    {
        long? ukprn = notification.Ukprn;
        if (!ukprn.HasValue && request is not null)
        {
            ukprn = request.Ukprn;
        }

        return ukprn;
    }

    private static void UpdateNotification(Notification notification, Request? request)
    {
        if (notification.Ukprn is null && request is not null)
        {
            notification.Ukprn = request.Ukprn;
        }

        notification.SentTime = DateTime.UtcNow;
    }

    private async Task<ProviderEmailRequest> CreateProviderEmailRequest(Notification notification, CancellationToken cancellationToken)
    {
        string templateId = GetTemplateId(notification);

        Dictionary<string, string> emailTokens = await _notificationTokenService.GetEmailTokens(notification, cancellationToken);

        return new ProviderEmailRequest
        {
            TemplateId = templateId,
            Tokens = emailTokens
        };
    }

    private async Task<SendEmailCommand> CreateEmployerEmailRequest(Notification notification, CancellationToken cancellationToken)
    {
        string templateId = GetTemplateId(notification);

        Dictionary<string, string> emailTokens = await _notificationTokenService.GetEmailTokens(notification, cancellationToken);

        return new SendEmailCommand(templateId, notification.EmailAddress, emailTokens);
    }

    private string GetTemplateId(Notification notification)
    {
        TemplateConfiguration templateConfiguration = _notificationsConfiguration.NotificationTemplates.Find(a => a.TemplateName == notification.TemplateName)!;

        if (templateConfiguration is null)
        {
            throw new ArgumentNullException($"Unable to find configuration for template {notification.TemplateName}");
        }

        return templateConfiguration.TemplateId;
    }
}
