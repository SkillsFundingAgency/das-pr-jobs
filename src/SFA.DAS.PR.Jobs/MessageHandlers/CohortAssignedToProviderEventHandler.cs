using Microsoft.Extensions.Logging;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Models;
using SFA.DAS.PR.Jobs.OuterApi.Responses;
using SFA.DAS.PR.Jobs.Services;

namespace SFA.DAS.PR.Jobs.MessageHandlers;

public sealed class CohortAssignedToProviderEventHandler(
    ILogger<CohortAssignedToProviderEventHandler> _logger,
    ICommitmentsV2ApiClient _commitmentsV2ApiClient,
    IProviderRelationshipsDataContext _providerRelationshipsDataContext,
    IRelationshipService _relationshipService
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

        bool relationshipCreated = await _relationshipService.CreateRelationship(
            relationshipModel,
            context.CancellationToken
        );

        CreateJobAudit(_providerRelationshipsDataContext, message, context, relationshipCreated);

        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "{EventHandlerName} completed at: {TimeStamp}. AccountProviderLegalEntity created successfully.",
            nameof(CohortAssignedToProviderEventHandler),
            DateTime.UtcNow
        );
    }

    private static void CreateJobAudit(IProviderRelationshipsDataContext _providerRelationshipsDataContext, CohortAssignedToProviderEvent message, IMessageHandlerContext context, bool relationshipCreated)
    {
        var notes = relationshipCreated ? "Relationship created" : "Relationship not created";

        JobAudit jobAudit = new JobAudit(
            nameof(CohortAssignedToProviderEventHandler),
            new EventHandlerJobInfo<CohortAssignedToProviderEvent>(context.MessageId, message, true, notes));

        _providerRelationshipsDataContext.JobAudits.Add(jobAudit);
    }
}
