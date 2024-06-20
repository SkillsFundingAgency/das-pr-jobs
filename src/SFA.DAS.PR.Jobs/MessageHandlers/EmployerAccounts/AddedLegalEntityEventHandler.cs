using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;
public class AddedLegalEntityEventHandler(IProviderRelationshipsDataContext _providerRelationshipsDataContext, ILogger<AddedLegalEntityEventHandler> _logger) : IHandleMessages<AddedLegalEntityEvent>
{
    public const string AccountLegalEntityAlreadyExistsFailureReason = "Account legal entity already exists";

    public async Task Handle(AddedLegalEntityEvent message, IMessageHandlerContext context)
    {
        var accountLegalEntity = await _providerRelationshipsDataContext
            .AccountLegalEntities
            .FirstOrDefaultAsync(a => a.AccountId == message.AccountId && a.Id == message.AccountLegalEntityId, context.CancellationToken);

        if (accountLegalEntity != null)
        {
            _logger.LogWarning("Legal entity with Id:{LegalEntityId} already exists", message.LegalEntityId);
            JobAudit jobAudit = new(
                nameof(AddedLegalEntityEventHandler),
                new EventHandlerJobInfo<AddedLegalEntityEvent>(context.MessageId, message, false, AccountLegalEntityAlreadyExistsFailureReason));
            _providerRelationshipsDataContext.JobAudits.Add(jobAudit);
        }
        else
        {
            _providerRelationshipsDataContext
                .AccountLegalEntities
                .Add(new()
                {
                    Id = message.AccountLegalEntityId,
                    AccountId = message.AccountId,
                    PublicHashedId = message.AccountLegalEntityPublicHashedId,
                    Name = message.OrganisationName,
                    Created = message.Created
                });
            _providerRelationshipsDataContext
                .JobAudits
                .Add(new(nameof(AddedLegalEntityEventHandler),
                    new EventHandlerJobInfo<AddedLegalEntityEvent>(context.MessageId, message, true, null)));
            _logger.LogInformation("Created new legal entity with Id: {LegalEntityId} associated with employer account with Id:{EmployerAccountId}", message.LegalEntityId, message.AccountId);
        }
        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);
    }
}
