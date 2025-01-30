using Esfa.Recruit.Vacancies.Client.Domain.Events;
using Microsoft.Extensions.Logging;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Models;
using SFA.DAS.PR.Jobs.Services;

namespace SFA.DAS.PR.Jobs.MessageHandlers.Recruit;

public sealed class VacancyApprovedEventHandler(
    ILogger<VacancyApprovedEventHandler> _logger,
    IProviderRelationshipsDataContext _providerRelationshipsDataContext,
    IRelationshipService _relationshipService
) : IHandleMessages<VacancyApprovedEvent>
{
    public async Task Handle(VacancyApprovedEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Listening to {EventType}", nameof(VacancyApprovedEvent));

        bool relationshipCreated = await _relationshipService.CreateRelationship(CreateRelationshipModel(message), context.CancellationToken);

        CreateJobAudit(_providerRelationshipsDataContext, message, context, relationshipCreated);

        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "{EventHandlerName} completed at: {TimeStamp}. AccountProviderLegalEntity created successfully.",
            nameof(VacancyApprovedEvent),
            DateTime.UtcNow
        );
    }

    private static void CreateJobAudit(IProviderRelationshipsDataContext _providerRelationshipsDataContext, VacancyApprovedEvent message, IMessageHandlerContext context, bool relationshipCreated)
    {
        var notes = relationshipCreated ? "Relationship created" : "Relationship not created";

        JobAudit jobAudit = new JobAudit(
            nameof(VacancyApprovedEventHandler),
            new EventHandlerJobInfo<VacancyApprovedEvent>(context.MessageId, message, true, notes));

        _providerRelationshipsDataContext.JobAudits.Add(jobAudit);
    }

    private static RelationshipModel CreateRelationshipModel(VacancyApprovedEvent liveVacancy)
    {
        return new RelationshipModel(
            null,
            liveVacancy.AccountLegalEntityPublicHashedId,
            liveVacancy.Ukprn,
            null,
            "LinkedAccountRecruit",
            nameof(PermissionAction.RecruitRelationship)
        );
    }
}
