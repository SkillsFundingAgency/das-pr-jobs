using Esfa.Recruit.Vacancies.Client.Domain.Events;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using NServiceBus.Features;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Models.Recruit;
using System.Security.Policy;

namespace SFA.DAS.PR.Jobs.MessageHandlers.Recruit;

public sealed class VacancyApprovedEventHandler(ILogger<VacancyApprovedEventHandler> _logger, IRecruitApiClient _recruitApiClient, IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IHandleMessages<VacancyApprovedEvent>
{
    public async Task Handle(VacancyApprovedEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Listening to {EventType}", nameof(VacancyApprovedEvent));

        //Need to add role assignment to recruit API
        // das-config

        //Add rest client to be able to talk to recruit API

        GetLiveVacancyQueryResponse recruitResponse = await _recruitApiClient.GetLiveVacancies(message.VacancyReference, context.CancellationToken);

        if(recruitResponse.ResultCode != ResponseCode.Success)
        {
            return;
        }



        //Check if relation ship exists, if not

        //create AccountProvider if not exists
        //            create APLE

        //create Audit with correct action

        //send notification to provider(The notification is being sent to the provider as employer has created the advert. Provider cannot create advert if the relationship does not exist.)
    }
}
