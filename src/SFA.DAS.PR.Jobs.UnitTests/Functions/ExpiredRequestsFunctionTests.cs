using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Configuration;
using SFA.DAS.PR.Jobs.Functions.Requests;

namespace SFA.DAS.PR.Jobs.UnitTests.Functions;

[TestFixture]
public sealed class ExpiredRequestsFunctionTests
{
    private ExpiredRequestsFunction _function;
    private Mock<ILogger<ExpiredRequestsFunction>> _loggerMock;
    private Mock<IProviderRelationshipsDataContext> _dbContextMock;
    private Mock<IRequestsRepository> _requestsRepositoryMock;
    private Mock<IJobAuditRepository> _jobAuditRepositoryMock;
    private Mock<IOptions<NotificationsConfiguration>> _notificationsConfigMock;
    private NotificationsConfiguration _notificationsConfiguration;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ExpiredRequestsFunction>>();
        _dbContextMock = new Mock<IProviderRelationshipsDataContext>();

        _dbContextMock.Setup(x =>
            x.Notifications
        ).Returns(new Mock<DbSet<Notification>>().Object);

        _requestsRepositoryMock = new Mock<IRequestsRepository>();
        _jobAuditRepositoryMock = new Mock<IJobAuditRepository>();
        _notificationsConfigMock = new Mock<IOptions<NotificationsConfiguration>>();
        _notificationsConfiguration = new NotificationsConfiguration { RequestExpiry = 30 };
        _notificationsConfigMock.Setup(n => n.Value).Returns(_notificationsConfiguration);

        _function = new ExpiredRequestsFunction(
            _loggerMock.Object,
            _dbContextMock.Object,
            _notificationsConfigMock.Object,
            _requestsRepositoryMock.Object,
            _jobAuditRepositoryMock.Object
        );

        _cancellationToken = CancellationToken.None;
    }

    [Test]
    public async Task Run_NoExpiredRequests_Returns()
    {
        _requestsRepositoryMock
            .Setup(repo => repo.GetExpiredRequests(
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()
            )
        )
        .Returns((int id, CancellationToken token) => new ValueTask<IEnumerable<Request>>([]));

        await _function.Run(
            new TimerInfo(), 
            new Mock<FunctionContext>().Object, 
            _cancellationToken
        );

        _dbContextMock.Verify(x => 
            x.Notifications.AddRangeAsync(
                It.IsAny<IEnumerable<Notification>>(), 
                _cancellationToken
            ), Times.Never
        );

        _jobAuditRepositoryMock.Verify(x => 
            x.CreateJobAudit(
                It.IsAny<JobAudit>(), 
                _cancellationToken
            ), 
            Times.Never
        );

        _dbContextMock.Verify(x => x.SaveChangesAsync(_cancellationToken), Times.Never);
    }

    [Test]
    public async Task Run_WithExpiredRequests_ProcessesRequestsAndSaves()
    {
        var expiredRequest = new Request
        {
            Id = Guid.NewGuid(),
            RequestType = RequestType.CreateAccount,
            Ukprn = 12345678,
            AccountLegalEntityId = 1,
            RequestedDate = DateTime.UtcNow.AddDays(-40),
            Status = RequestStatus.New
        };

        _requestsRepositoryMock
            .Setup(repo => repo.GetExpiredRequests(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()
            )
        )
        .Returns((int id, CancellationToken token) => new ValueTask<IEnumerable<Request>>([expiredRequest]));

        await _function.Run(
            new TimerInfo(), 
            new Mock<FunctionContext>().Object, 
            _cancellationToken
        );

        _requestsRepositoryMock.Verify(x => x.GetExpiredRequests(_notificationsConfiguration.RequestExpiry, _cancellationToken), Times.Once);
        _jobAuditRepositoryMock.Verify(x => x.CreateJobAudit(It.IsAny<JobAudit>(), _cancellationToken), Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(_cancellationToken), Times.Once);
    }

    [Test]
    public async Task Run_ExpiredRequest_SetsStatusToExpired()
    {
        var expiredRequest = new Request
        {
            Id = Guid.NewGuid(),
            RequestType = RequestType.Permission,
            Ukprn = 12345678,
            AccountLegalEntityId = 1,
            RequestedDate = DateTime.UtcNow.AddDays(-40),
            Status = RequestStatus.New
        };

        _requestsRepositoryMock
            .Setup(repo => repo.GetExpiredRequests(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()
            )
        )
        .Returns((int id, CancellationToken token) => new ValueTask<IEnumerable<Request>>([expiredRequest]));

        await _function.Run(
            new TimerInfo(), 
            new Mock<FunctionContext>().Object, 
            _cancellationToken
        );

        Assert.That(expiredRequest.Status, Is.EqualTo(RequestStatus.Expired));

        _dbContextMock.Verify(x => x.SaveChangesAsync(_cancellationToken), Times.Once);
    }

    [Test]
    public async Task Run_ExpiredRequest_RequestTypePermission_CreateCorrectNotification()
    {
        var expiredRequest = new Request()
        {
            Id = Guid.NewGuid(),
            RequestType = RequestType.Permission,
            Ukprn = 12345678,
            AccountLegalEntityId = 1,
            RequestedDate = DateTime.UtcNow.AddDays(-40),
            Status = RequestStatus.New,
            RequestedBy = Guid.NewGuid().ToString()
        };

        using var context = DbContextHelper.CreateInMemoryDbContext()
            .AddRequest(expiredRequest)
            .PersistChanges();

        var function = CreateExpiredRequestsFunction(context);
        await function.Run(new TimerInfo(), new Mock<FunctionContext>().Object, _cancellationToken);

        var request = context.Requests.First();
        var notification = context.Notifications.First();

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(RequestStatus.Expired));
            Assert.That(notification.NotificationType, Is.EqualTo(nameof(NotificationType.Provider)));
            Assert.That(notification.TemplateName, Is.EqualTo("UpdatePermissionExpired"));
            Assert.That(notification.Ukprn, Is.EqualTo(request.Ukprn));
            Assert.That(notification.CreatedBy, Is.EqualTo("PR Jobs: UpdatePermissionExpired"));
            Assert.That(notification.RequestId, Is.EqualTo(request.Id));
            Assert.That(notification.AccountLegalEntityId, Is.EqualTo(request.AccountLegalEntityId));
        });
    }

    [Test]
    public async Task Run_ExpiredRequest_RequestTypeCreateAccount_CreateCorrectNotification()
    {
        var expiredRequest = new Request()
        {
            Id = Guid.NewGuid(),
            RequestType = RequestType.CreateAccount,
            Ukprn = 12345678,
            AccountLegalEntityId = 1,
            RequestedDate = DateTime.UtcNow.AddDays(-40),
            Status = RequestStatus.New,
            RequestedBy = Guid.NewGuid().ToString()
        };

        using var context = DbContextHelper.CreateInMemoryDbContext()
            .AddRequest(expiredRequest)
            .PersistChanges();

        var function = CreateExpiredRequestsFunction(context);
        await function.Run(new TimerInfo(), new Mock<FunctionContext>().Object, _cancellationToken);

        var request = context.Requests.First();
        var notification = context.Notifications.First();

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(RequestStatus.Expired));
            Assert.That(notification.NotificationType, Is.EqualTo(nameof(NotificationType.Provider)));
            Assert.That(notification.TemplateName, Is.EqualTo("CreateAccountExpired"));
            Assert.That(notification.Ukprn, Is.EqualTo(request.Ukprn));
            Assert.That(notification.CreatedBy, Is.EqualTo("PR Jobs: CreateAccountExpired"));
            Assert.That(notification.RequestId, Is.EqualTo(request.Id));
            Assert.That(notification.AccountLegalEntityId, Is.EqualTo(request.AccountLegalEntityId));
        });
    }

    [Test]
    public async Task Run_ExpiredRequest_RequestTypeAddAccount_CreateCorrectNotification()
    {
        var expiredRequest = new Request()
        {
            Id = Guid.NewGuid(),
            RequestType = RequestType.AddAccount,
            Ukprn = 12345678,
            AccountLegalEntityId = 1,
            RequestedDate = DateTime.UtcNow.AddDays(-40),
            Status = RequestStatus.New,
            RequestedBy = Guid.NewGuid().ToString()
        };

        using var context = DbContextHelper.CreateInMemoryDbContext()
            .AddRequest(expiredRequest)
            .PersistChanges();

        var function = CreateExpiredRequestsFunction(context);

        await function.Run(new TimerInfo(), new Mock<FunctionContext>().Object, _cancellationToken);

        var request = context.Requests.First();
        var notification = context.Notifications.First();

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(RequestStatus.Expired));
            Assert.That(notification.NotificationType, Is.EqualTo(nameof(NotificationType.Provider)));
            Assert.That(notification.TemplateName, Is.EqualTo("AddAccountExpired"));
            Assert.That(notification.Ukprn, Is.EqualTo(request.Ukprn));
            Assert.That(notification.CreatedBy, Is.EqualTo("PR Jobs: AddAccountExpired"));
            Assert.That(notification.RequestId, Is.EqualTo(request.Id));
            Assert.That(notification.AccountLegalEntityId, Is.EqualTo(request.AccountLegalEntityId));
        });
    }

    private ExpiredRequestsFunction CreateExpiredRequestsFunction(ProviderRelationshipsDataContext context)
    {
        NotificationRepository notificationRepository = new NotificationRepository(context);
        RequestsRepository requestRepository = new RequestsRepository(context);
        JobAuditRepository jobAuditRepository = new JobAuditRepository(context);
        return new ExpiredRequestsFunction(
            _loggerMock.Object,
            context,
            _notificationsConfigMock.Object,
            requestRepository,
            jobAuditRepository
        );
    }
}
