﻿using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.PAS.Account.Api.Types;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Configuration;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Services;

namespace SFA.DAS.PR.Jobs.Functions.Notifications;

public class SendNotificationsFunction
{
    private readonly ILogger<SendNotificationsFunction> _logger;
    private readonly IProviderRelationshipsDataContext _providerRelationshipsDataContext;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationTokenService _notificationTokenService;
    private readonly IPasAccountApiClient _pasAccountApiClient;
    private readonly NotificationsConfiguration _notificationsConfiguration;

    public SendNotificationsFunction(
        ILogger<SendNotificationsFunction> logger,
        IProviderRelationshipsDataContext providerRelationshipsDataContext,
        INotificationRepository notificationRepository,
        INotificationTokenService notificationTokenService,
        IPasAccountApiClient pasAccountApiClient,
        IOptions<NotificationsConfiguration> notificationsConfigurationOptions
    )
    {
        _logger = logger;
        _providerRelationshipsDataContext = providerRelationshipsDataContext;
        _notificationRepository = notificationRepository;
        _notificationTokenService = notificationTokenService;
        _pasAccountApiClient = pasAccountApiClient;
        _notificationsConfiguration = notificationsConfigurationOptions.Value;
    }

    [Function(nameof(SendNotificationsFunction))]
    public async Task Run([TimerTrigger("%SendNotificationsFunctionSchedule%", RunOnStartup = false)] TimerInfo timer, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{FunctionName} has been triggered.", nameof(SendNotificationsFunction));

        List<Notification> notifications = await _notificationRepository.GetPendingNotifications(
            _notificationsConfiguration.BatchSize,
            NotificationType.Provider,
            cancellationToken
        );

        int processedCount = 0;

        if (notifications.Any())
        {
            foreach (Notification notification in notifications)
            {
                processedCount += await TryProcessProviderNotification(notification, cancellationToken);
            }

            await _providerRelationshipsDataContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("{FunctionName} - Processed {ProcessedCount} notifications.", nameof(SendNotificationsFunction), processedCount);
    }

    private async Task<int> TryProcessProviderNotification(Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            ProviderEmailRequest providerEmailRequest = await CreateProviderEmailRequest(notification, cancellationToken);
            await _pasAccountApiClient.SendEmailToAllProviderRecipients(notification.Ukprn!.Value, providerEmailRequest, cancellationToken);
            notification.SentTime = DateTime.UtcNow;

            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending out notification with id: {NotificationId}", notification.Id);

            return 0;
        }
    }

    private async Task<ProviderEmailRequest> CreateProviderEmailRequest(Notification notification, CancellationToken cancellationToken)
    {
        TemplateConfiguration templateConfiguration = _notificationsConfiguration.NotificationTemplates.Find(a => a.TemplateName == notification.TemplateName)!;

        Dictionary<string, string> emailTokens = await _notificationTokenService.GetEmailTokens(notification, cancellationToken);

        return new ProviderEmailRequest
        {
            TemplateId = templateConfiguration.TemplateId,
            Tokens = emailTokens
        };
    }
}
