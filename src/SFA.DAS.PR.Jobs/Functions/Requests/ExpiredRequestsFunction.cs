using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Configuration;

namespace SFA.DAS.PR.Jobs.Functions.Requests;

public sealed class ExpiredRequestsFunction
{
    private readonly ILogger<ExpiredRequestsFunction> _logger;
    private readonly IProviderRelationshipsDataContext _dbContext;
    private readonly IOptions<NotificationsConfiguration> _notificationsConfigurationOptions;
    private readonly IRequestsRepository _requestsRepository;
    private readonly IJobAuditRepository _jobAuditRepostory;
    

    public ExpiredRequestsFunction(
        ILogger<ExpiredRequestsFunction> logger,
        IProviderRelationshipsDataContext dbContext,
        IOptions<NotificationsConfiguration> notificationsConfigurationOptions,
        IRequestsRepository requestsRepository,
        IJobAuditRepository jobAuditRepostory
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _notificationsConfigurationOptions = notificationsConfigurationOptions;
        _requestsRepository = requestsRepository;
        _jobAuditRepostory = jobAuditRepostory;
    }

    [Function(nameof(ExpiredRequestsFunction))]
    public async Task Run(
    [TimerTrigger("%ExpiredRequestsFunctionSchedule%", RunOnStartup = true)] TimerInfo timer,
    FunctionContext executionContext,
    CancellationToken cancellationToken)
    {
        _logger.LogInformation("{FunctionName} has been triggered.", nameof(ExpiredRequestsFunction));

        var expiredRequests = await _requestsRepository.GetExpiredRequests(
            _notificationsConfigurationOptions.Value.RequestExpiry,
            cancellationToken
        );

        if (!expiredRequests.Any())
        {
            return;
        }

        List<Notification> notifications = [];

        foreach (var request in expiredRequests)
        {
            UpdateRequestStatusToExpired(request);
            var notification = CreateNotificationsForRequest(request);
            if(notification is not null)
            {
                notifications.Add(notification);
            }
        }

        if (notifications.Any())
        {
            await _dbContext.Notifications.AddRangeAsync(notifications, cancellationToken);
        }

        await _jobAuditRepostory.CreateJobAudit(
            CreateJobAudit(expiredRequests.Select(r => r.Id).ToArray()),
            cancellationToken
        );

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("{FunctionName} - Processed {ProcessedCount} expired requests.", nameof(ExpiredRequestsFunction), expiredRequests.Count());
    }

    private void UpdateRequestStatusToExpired(Request request)
    {
        request.UpdatedDate = DateTime.UtcNow;
        request.Status = RequestStatus.Expired;
    }

    private Notification? CreateNotificationsForRequest(Request request)
    {
        switch (request.RequestType)
        {
            case RequestType.CreateAccount:
                return CreateNotification("CreateAccountExpired", "PR Jobs: CreateAccountExpired", request);
            case RequestType.AddAccount:
                return CreateNotification("AddAccountExpired", "PR Jobs: AddAccountExpired", request);
            case RequestType.Permission:
                return CreateNotification("UpdatePermissionExpired", "PR Jobs: UpdatePermissionExpired", request);
            default:
                return null;
        }
    }

    private JobAudit CreateJobAudit(Guid[] requestIds)
    {
        return new JobAudit
        {
            JobName = nameof(ExpiredRequestsFunction),
            JobInfo = $"[{string.Join(',', requestIds)}]",
            ExecutedOn = DateTime.UtcNow
        };
    }

    private Notification CreateNotification(string templateName, string createdBy, Request request)
    {
        return new Notification
        {
            TemplateName = templateName,
            NotificationType = nameof(NotificationType.Provider),
            Ukprn = request.Ukprn,
            CreatedBy = createdBy,
            RequestId = request.Id,
            AccountLegalEntityId = request.AccountLegalEntityId
        };
    }
}
