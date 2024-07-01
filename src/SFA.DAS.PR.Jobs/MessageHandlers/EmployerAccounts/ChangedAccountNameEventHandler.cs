using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;

public record AccountUpdateEventOutcome(bool IsValid, string FailureReason);

public class ChangedAccountNameEventHandler(IProviderRelationshipsDataContext _providerRelationshipsDataContext, ILogger<ChangedAccountNameEventHandler> _logger) : IHandleMessages<ChangedAccountNameEvent>
{
    public const string AccountNullFailureReason = "Update Invalid: Account is null";

    public const string AcountNameMatchFailureReason = "Update Invalid: Message name matches account name";

    public const string AccountDateFailureReason = "Update Invalid: Message created date exceeds account updated date";
    public async Task Handle(ChangedAccountNameEvent message, IMessageHandlerContext context)
    {
        Account? account = await _providerRelationshipsDataContext.Accounts.FirstOrDefaultAsync(a => a.Id == message.AccountId, context.CancellationToken);

        JobAudit? jobAudit = null;

        AccountUpdateEventOutcome accountUpdateEventOutcome = AccountUpdateIsValid(message, account);

        if (accountUpdateEventOutcome.IsValid)
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
                new EventHandlerJobInfo<ChangedAccountNameEvent>(context.MessageId, message, false, accountUpdateEventOutcome.FailureReason)
            );
        }

        await _providerRelationshipsDataContext.JobAudits.AddAsync(jobAudit!, context.CancellationToken);
        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);
    }

    private static AccountUpdateEventOutcome AccountUpdateIsValid(ChangedAccountNameEvent message, Account? account)
    {
        if (account == null)
        {
            return new(false, AccountNullFailureReason);
        }

        if (account.Name == message.CurrentName)
        {
            return new(false, AcountNameMatchFailureReason);
        }

        if (account.Updated.HasValue && message.Created < account.Updated)
        {
            return new(false, AccountDateFailureReason);
        }

        return new(true, string.Empty);
    }
}