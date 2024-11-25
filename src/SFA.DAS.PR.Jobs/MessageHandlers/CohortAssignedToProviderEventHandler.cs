using Microsoft.Extensions.Logging;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.OuterApi.Responses;
using SFA.DAS.PR.Jobs.Services;
using System.Text.Json;

namespace SFA.DAS.PR.Jobs.MessageHandlers;

public sealed class CohortAssignedToProviderEventHandler(
    ILogger<CohortAssignedToProviderEventHandler> _logger,
    ICommitmentsV2ApiClient _commitmentsV2ApiClient,
    IProviderRelationshipsDataContext _providerRelationshipsDataContext,
    IRelationshipService _relationshipService,
    IJobAuditRepository _jobAuditRepository
) : IHandleMessages<CohortAssignedToProviderEvent>
{
    public async Task Handle(CohortAssignedToProviderEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("CohortAssignedToProviderEventHandler started at: {TimeStamp}", DateTime.UtcNow);

        CohortModel cohort = await _commitmentsV2ApiClient.GetCohortDetails(message.CohortId, CancellationToken.None);

        RelationshipModel relationshipModel = new RelationshipModel(
            cohort.AccountLegalEntityId, 
            null,
            cohort.ProviderId,
            null,
            "LinkedAccountCohort",
            nameof(PermissionAction.ApprovalsRelationship)
        );

        await _relationshipService.CreateRelationship<CohortAssignedToProviderEventHandler>(
            _logger,
            relationshipModel,
            context.CancellationToken
        );

        await _jobAuditRepository.CreateJobAudit(
            CreateJobAudit(message),
            context.CancellationToken
        );

        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "{EventHandlerName} completed at: {TimeStamp}. AccountProviderLegalEntity created successfully.",
            nameof(CohortAssignedToProviderEventHandler),
            DateTime.UtcNow
        );
    }

    private static JobAudit CreateJobAudit(CohortAssignedToProviderEvent message)
    {
        return new JobAudit
        {
            JobName = nameof(CohortAssignedToProviderEvent),
            JobInfo = $"{JsonSerializer.Serialize(message)}",
            ExecutedOn = DateTime.UtcNow
        };
    }
}
