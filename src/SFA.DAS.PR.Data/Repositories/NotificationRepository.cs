﻿using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface INotificationRepository
{
    Task<List<Notification>> GetPendingNotifications(int batchSize, CancellationToken cancellationToken);
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
}
