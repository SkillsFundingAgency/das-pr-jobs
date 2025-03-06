using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;

public record UpdatedLegalEntityEventOutcome(bool IsValid, string FailureMessage);

public class UpdatedLegalEntityEventHandler(IProviderRelationshipsDataContext _providerRelationshipsDataContext, ILogger<UpdatedLegalEntityEventHandler> _logger) : IHandleMessages<UpdatedLegalEntityEvent>
{
    public const string AccountLegalEntityNullFailureReason = "Update Invalid: Account legal entity does not exist";

    public const string AccountLegalEntityDeleteFailureReason = "Update Invalid: Account legal entity has been deleted";

    public const string AccountLegalEntityNameMatchFailureReason = "Update Invalid: Message name matches account legal entity name";

    public const string AccountLegalEntityDateFailureReason = "Update Invalid: Message Created date exceeds account legal entity updated date";

    public async Task Handle(UpdatedLegalEntityEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("{MessageHandlerName} was triggered by MessageId:{MessageId} for AccountLegalEntityId:{AccountLegalEntityId}", nameof(UpdatedLegalEntityEventHandler), context.MessageId, message.AccountLegalEntityId);

        AccountLegalEntity? accountLegalEntity = await _providerRelationshipsDataContext
            .AccountLegalEntities
            .FirstOrDefaultAsync(a => a.Id == message.AccountLegalEntityId, context.CancellationToken);

        JobAudit? jobAudit = null;

        UpdatedLegalEntityEventOutcome failureOutcome = AccountLegalEntityUpdateIsValid(message, accountLegalEntity);

        if (failureOutcome.IsValid)
        {
            accountLegalEntity!.Name = message.Name;
            accountLegalEntity!.Updated = message.Created;

            jobAudit = new(
                nameof(UpdatedLegalEntityEventHandler),
                new EventHandlerJobInfo<UpdatedLegalEntityEvent>(context.MessageId, message, true, null)
            );
        }
        else
        {
            _logger.LogWarning("AccountLegalEntity update for AccountLegalEntityId:{AccountLegalEntityId} is not valid", message.AccountLegalEntityId);

            jobAudit = new(
                nameof(UpdatedLegalEntityEventHandler),
                new EventHandlerJobInfo<UpdatedLegalEntityEvent>(context.MessageId, message, false, failureOutcome.FailureMessage)
            );
        }

        await _providerRelationshipsDataContext.JobAudits.AddAsync(jobAudit!, context.CancellationToken);
        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);
    }

    private static UpdatedLegalEntityEventOutcome AccountLegalEntityUpdateIsValid(UpdatedLegalEntityEvent message, AccountLegalEntity? accountLegalEntity)
    {
        if (accountLegalEntity == null)
        {
            return new(false, AccountLegalEntityNullFailureReason);
        }

        if (accountLegalEntity.Deleted.HasValue)
        {
            return new(false, AccountLegalEntityDeleteFailureReason);
        }

        if (accountLegalEntity.Name == message.Name)
        {
            return new(false, AccountLegalEntityNameMatchFailureReason);
        }

        if (accountLegalEntity.Updated.HasValue && message.Created < accountLegalEntity.Updated)
        {
            return new(false, AccountLegalEntityDateFailureReason);
        }

        return new(true, string.Empty);
    }
}
