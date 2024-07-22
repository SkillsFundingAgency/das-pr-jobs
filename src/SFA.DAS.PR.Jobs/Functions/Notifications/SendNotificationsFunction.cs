using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.Notifications.Messages.Commands;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Configuration;
using SFA.DAS.PR.Jobs.Services;

namespace SFA.DAS.PR.Jobs.Functions.Notifications;

public class SendNotificationsFunction
{
    private readonly ILogger<SendNotificationsFunction> _logger;
    private readonly NotificationsConfiguration _notificationsConfiguration;
    private readonly IFunctionEndpoint _functionEndpoint;
    private readonly IProviderRelationshipsDataContext _providerRelationshipsDataContext;
    private readonly INotificationRepository _notificationRepository;
    private readonly ITokenService _tokenService;

    public SendNotificationsFunction(
        ILogger<SendNotificationsFunction> logger,
        IConfiguration configuration,
        IFunctionEndpoint functionEndpoint,
        IProviderRelationshipsDataContext providerRelationshipsDataContext,
        INotificationRepository notificationRepository,
        ITokenService tokenService
    )
    {
        _logger = logger;
        _functionEndpoint = functionEndpoint;
        _providerRelationshipsDataContext = providerRelationshipsDataContext;
        _notificationRepository = notificationRepository;
        _tokenService = tokenService;

        _notificationsConfiguration = new NotificationsConfiguration();
        configuration.GetSection("ApplicationConfiguration:Notifications").Bind(_notificationsConfiguration);
    }

    [Function(nameof(SendNotificationsFunction))]
    public async Task<int> Run([TimerTrigger("%SendNotificationsFunctionSchedule%", RunOnStartup = true)] TimerInfo timer, FunctionContext executionContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("{FunctionName} has been triggered.", nameof(SendNotificationsFunction));

        List<Notification> notifications = await _notificationRepository.GetPendingNotifications(
            _notificationsConfiguration.BatchSize, 
            NotificationType.Provider, 
            cancellationToken
        );

        if(notifications.Count == 0)
        {
            return 0;
        }

        int processedCount = 0;

        foreach (Notification notification in notifications)
        {
            processedCount += await ProcessNotification(notification, executionContext, cancellationToken);
        }

        await _providerRelationshipsDataContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("{FunctionName} - Processed {ProcessedCount} notifications.", nameof(SendNotificationsFunction), processedCount);

        return processedCount;
    }

    private async Task<int> ProcessNotification(Notification notification, FunctionContext executionContext, CancellationToken cancellationToken)
    {
        try
        {
            SendEmailCommand sendEmailCommand = await CreateSendEmailCommand(notification, cancellationToken);
            await _functionEndpoint.Send(sendEmailCommand, executionContext, cancellationToken);
            notification.SentTime = DateTime.UtcNow;

            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending out notification with id: {notificationId}", notification.Id);

            return 0;
        }
    }

    private async Task<SendEmailCommand> CreateSendEmailCommand(Notification notification, CancellationToken cancellationToken)
    {
        TemplateConfiguration? templateConfiguration = _notificationsConfiguration.NotificationTemplates.Find(a => a.TemplateName == notification.TemplateName);

        if(templateConfiguration == null || string.IsNullOrWhiteSpace(templateConfiguration.TemplateId))
        {
            throw new ArgumentNullException($"Template {notification.TemplateName} does not exist in the configuration for notification {notification.Id}.");
        }

        Dictionary<string, string> emailTokens = await _tokenService.GetEmailTokens(notification, cancellationToken); 

        return new SendEmailCommand(templateConfiguration.TemplateId, notification.EmailAddress, emailTokens);
    }
}
