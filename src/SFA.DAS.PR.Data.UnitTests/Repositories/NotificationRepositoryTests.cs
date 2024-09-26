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
    public async Task NotificationsRepository_GetPendingNotifications_ByProvider_Returns_Empty()
    {
        Notification notification = NotificationData.Create(Guid.NewGuid(), NotificationType.Employer, 10000001, 1, "TemplateName");

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddNotification(notification)
            .PersistChanges();

        NotificationRepository sut = new NotificationRepository(context);
        var result = await sut.GetPendingNotifications(100, CancellationToken.None);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task NotificationsRepository_GetPendingNotifications_Returns_Empty()
    {
        using var context = DbContextHelper.CreateInMemoryDbContext();

        NotificationRepository sut = new NotificationRepository(context);
        var result = await sut.GetPendingNotifications(100, CancellationToken.None);
        Assert.That(result, Is.Empty);
    }
}
