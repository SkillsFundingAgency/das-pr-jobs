using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SFA.DAS.PR.Jobs.Functions.Notifications;

public sealed class NotificationsCleanUpFunction
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IProviderRelationshipsDataContext _providerRelationshipsDataContext;
    private readonly NotificationsConfiguration _notificationsConfiguration;
    public NotificationsCleanUpFunction(
        INotificationRepository notificationRepository, 
        IProviderRelationshipsDataContext providerRelationshipsDataContext, 
        IOptions<NotificationsConfiguration> notificationsConfiguration
    )
    {
        _notificationRepository = notificationRepository;
        _providerRelationshipsDataContext = providerRelationshipsDataContext;
        _notificationsConfiguration = notificationsConfiguration.Value;
    }

    [Function(nameof(NotificationsCleanUpFunction))]
    public async Task Run([TimerTrigger("%NotificationsCleanUpFunctionSchedule%", RunOnStartup = false)] TimerInfo timer, FunctionContext executionContext, CancellationToken cancellationToken)
    {
        var notifications = await _notificationRepository.GetExpiredNotifications(
            _notificationsConfiguration.NotificationRetentionDays,
            executionContext.CancellationToken
        );

        if(notifications.Count < 1)
        {
            return;
        }

        _notificationRepository.DeleteNotifications(notifications);

        await _providerRelationshipsDataContext.SaveChangesAsync(cancellationToken);
    }
}
