using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface INotificationRepository
{
    Task<List<Notification>> GetPendingNotifications(int batchSize, NotificationType notificationType, CancellationToken cancellationToken);
}

public class NotificationRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : INotificationRepository
{
    public async Task<List<Notification>> GetPendingNotifications(int batchSize, NotificationType notificationType, CancellationToken cancellationToken)
    {
        return await _providerRelationshipsDataContext.Notifications
                .Where(notification => notification.SentTime == null && notification.NotificationType == notificationType.ToString())
                .Take(batchSize)
                .ToListAsync(cancellationToken);
    }
}
