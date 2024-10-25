using NUnit.Framework;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.UnitTests;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Data.UnitTests.Repositories;

public class NotificationRepositoryTests
{
    [Test]
    public async Task NotificationsRepository_GetPendingNotifications_Returns_Success()
    {
        Notification notification = NotificationData.Create(Guid.NewGuid(), NotificationType.Provider, 10000001, 1, "TemplateName");

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddNotification(notification)
            .PersistChanges();

        NotificationRepository sut = new NotificationRepository(context);
        var result = await sut.GetPendingNotifications(100, CancellationToken.None);
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task NotificationsRepository_GetPendingNotifications_Returns_Empty()
    {
        using var context = DbContextHelper.CreateInMemoryDbContext();

        NotificationRepository sut = new NotificationRepository(context);
        var result = await sut.GetPendingNotifications(100, CancellationToken.None);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task NotificationsRepository_GetExpiredNotifications_Returns_Success()
    {
        Notification notification = NotificationData.Create(Guid.NewGuid(), NotificationType.Provider, 10000001, 1, "TemplateName");
        notification.CreatedDate = DateTime.UtcNow.AddDays(-366);

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddNotification(notification)
            .PersistChanges();

        NotificationRepository sut = new NotificationRepository(context);
        var result = await sut.GetExpiredNotifications(365, CancellationToken.None);
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task NotificationsRepository_GetExpiredNotifications_Returns_Empty()
    {
        using var context = DbContextHelper.CreateInMemoryDbContext();

        NotificationRepository sut = new NotificationRepository(context);
        var result = await sut.GetExpiredNotifications(365, CancellationToken.None);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task NotificationsRepository_GetExpiredNotifications_BoundaryLineExpired_Returns_Collection()
    {
        DateTime pastDate = DateTime.UtcNow.AddDays(-366);
        DateTime boundaryLine = new DateTime(pastDate.Year, pastDate.Month, pastDate.Day, 00, 00, 01);

        Notification notification = NotificationData.Create(
            Guid.NewGuid(), 
            NotificationType.Provider, 
            10000001, 
            1, 
            "TemplateName"
        );

        notification.CreatedDate = boundaryLine;

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddNotification(notification)
            .PersistChanges();

        NotificationRepository sut = new NotificationRepository(context);
        var result = await sut.GetExpiredNotifications(365, CancellationToken.None);
        Assert.That(result.Count, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task NotificationsRepository_GetExpiredNotifications_BoundaryLineExpired_Returns_Empty()
    {
        DateTime pastDate = DateTime.UtcNow.AddDays(-365);
        DateTime boundaryLine = new DateTime(pastDate.Year, pastDate.Month, pastDate.Day + 1, 00, 00, 01);

        Notification notification = NotificationData.Create(
            Guid.NewGuid(),
            NotificationType.Provider,
            10000001,
            1,
            "TemplateName"
        );

        notification.CreatedDate = boundaryLine;

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddNotification(notification)
            .PersistChanges();

        NotificationRepository sut = new NotificationRepository(context);
        var result = await sut.GetExpiredNotifications(365, CancellationToken.None);
        Assert.That(result.Count, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task NotificationsRepository_DeleteNotifications_Successful()
    {
        Notification notification = NotificationData.Create(
            Guid.NewGuid(),
            NotificationType.Provider,
            10000001,
            1,
            "TemplateName"
        );

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddNotification(notification)
            .PersistChanges();

        NotificationRepository sut = new NotificationRepository(context);
        sut.DeleteNotifications([notification]);
        await context.SaveChangesAsync(CancellationToken.None);

        var notifications = context.Notifications.ToList();

        Assert.That(notifications.Count, Is.EqualTo(0));
    }
}
