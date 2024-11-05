using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface INotificationRepository
{
    Task<List<Notification>> GetPendingNotifications(int batchSize, CancellationToken cancellationToken);
    Task<List<Notification>> GetExpiredNotifications(int retentionSpread, CancellationToken cancellationToken);
    void DeleteNotifications(IEnumerable<Notification> notifications);
}

public class NotificationRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : INotificationRepository
{
    public async Task<List<Notification>> GetPendingNotifications(int batchSize, CancellationToken cancellationToken)
    {
        return await _providerRelationshipsDataContext.Notifications
                .Where(notification => notification.SentTime == null)
                .OrderBy(notification => notification.CreatedDate)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
    }

    public async Task<List<Notification>> GetExpiredNotifications(int retentionSpread, CancellationToken cancellationToken)
    {
        return await _providerRelationshipsDataContext.Notifications
                .Where(notification => notification.CreatedDate.Date < DateTime.UtcNow.AddDays(-retentionSpread).Date)
                .ToListAsync(cancellationToken);
    }

    public void DeleteNotifications(IEnumerable<Notification> notifications)
    {
        _providerRelationshipsDataContext.Notifications.RemoveRange(notifications);
    }
}
