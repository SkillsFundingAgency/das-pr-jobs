using Esfa.Recruit.Vacancies.Client.Domain.Events;
using Microsoft.Extensions.Logging;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Models.Recruit;
using SFA.DAS.PR.Jobs.Services;
using System.Text.Json;

namespace SFA.DAS.PR.Jobs.MessageHandlers.Recruit;

public sealed class VacancyApprovedEventHandler(
    ILogger<VacancyApprovedEventHandler> _logger,
    IRecruitApiClient _recruitApiClient, 
    IProviderRelationshipsDataContext _providerRelationshipsDataContext,
    IJobAuditRepository _jobAuditRepository,
    IRelationshipService _relationshipService
) : IHandleMessages<VacancyApprovedEvent>
{
    public async Task Handle(VacancyApprovedEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Listening to {EventType}", nameof(VacancyApprovedEvent));

        LiveVacancyModel? liveVacancy = await _recruitApiClient.GetLiveVacancy(
            message.VacancyReference,
            context.CancellationToken
        );

        await _relationshipService.CreateRelationship(
            CreateRelationshipModel(liveVacancy),
            context.CancellationToken
        );

        await _jobAuditRepository.CreateJobAudit(
            CreateJobAudit(message),
            context.CancellationToken
        );

        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "{EventHandlerName} completed at: {TimeStamp}. AccountProviderLegalEntity created successfully.", 
            nameof(VacancyApprovedEvent),
            DateTime.UtcNow
        );
    }

    private static RelationshipModel CreateRelationshipModel(LiveVacancyModel liveVacancy)
    {
        return new RelationshipModel(
            null,
            liveVacancy.AccountLegalEntityPublicHashedId,
            liveVacancy.TrainingProvider!.Ukprn,
            liveVacancy.AccountPublicHashedId,
            "LinkedAccountRecruit",
            nameof(PermissionAction.RecruitRelationship)
        );
    }

    private static JobAudit CreateJobAudit(VacancyApprovedEvent message)
    {
        return new JobAudit
        {
            JobName = nameof(VacancyApprovedEvent),
            JobInfo = $"{JsonSerializer.Serialize(message)}",
            ExecutedOn = DateTime.UtcNow
        };
    }
}
