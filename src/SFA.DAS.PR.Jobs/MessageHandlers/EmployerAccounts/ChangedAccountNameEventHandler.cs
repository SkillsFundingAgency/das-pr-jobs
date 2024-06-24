using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;

public class ChangedAccountNameEventHandler(IProviderRelationshipsDataContext _providerRelationshipsDataContext, ILogger<ChangedAccountNameEventHandler> _logger) : IHandleMessages<ChangedAccountNameEvent>
{
    public const string AccountUpdateIsInvalidFailureReason = "The account does not exist or an update is not valid";
    public async Task Handle(ChangedAccountNameEvent message, IMessageHandlerContext context)
    {
        Account? account = await _providerRelationshipsDataContext.Accounts.FirstOrDefaultAsync(a => a.Id == message.AccountId, context.CancellationToken);

        JobAudit? jobAudit = null;

        if (AccountUpdateIsValid(account, message))
        {
            account!.Name = message.CurrentName;
            account!.Updated = message.Created;

            jobAudit = new(
                nameof(ChangedAccountNameEventHandler),
                new EventHandlerJobInfo<ChangedAccountNameEvent>(context.MessageId, message, true, null)
            );
        }
        else
        {
            _logger.LogWarning("Account update for AccountId:{EmployerAccountId} is not valid", message.AccountId);

            jobAudit = new(
                nameof(ChangedAccountNameEventHandler),
                new EventHandlerJobInfo<ChangedAccountNameEvent>(context.MessageId, message, false, AccountUpdateIsInvalidFailureReason)
            );
        }

        await _providerRelationshipsDataContext.JobAudits.AddAsync(jobAudit!, context.CancellationToken);
        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);
    }

    private bool AccountUpdateIsValid(Account? account, ChangedAccountNameEvent message)
    {
        return
            account != null &&
            (message.Created > account.Updated || !account.Updated.HasValue) &&
            message.CurrentName != account.Name;
    }
}