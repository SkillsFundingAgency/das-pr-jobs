using Microsoft.EntityFrameworkCore;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;
public class CreatedAccountEventHandler(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IHandleMessages<CreatedAccountEvent>
{
    public const string AccountAlreadyExistsFailureReason = "Account already exists";
    public async Task Handle(CreatedAccountEvent message, IMessageHandlerContext context)
    {
        var account = await _providerRelationshipsDataContext
            .Accounts
            .FirstOrDefaultAsync(a => a.Id == message.AccountId, context.CancellationToken);

        if (account != null)
        {
            JobAudit jobAudit = new(
                nameof(AddedLegalEntityEventHandler),
                new EventHandlerJobInfo<CreatedAccountEvent>(context.MessageId, message, false, AccountAlreadyExistsFailureReason));
            _providerRelationshipsDataContext.JobAudits.Add(jobAudit);
        }
        else
        {
            _providerRelationshipsDataContext
                .Accounts
                .Add(new()
                {
                    Id = message.AccountId,
                    HashedId = message.HashedId,
                    PublicHashedId = message.PublicHashedId,
                    Name = message.Name,
                    Created = message.Created
                });
            _providerRelationshipsDataContext
                .JobAudits
                .Add(new(nameof(AddedLegalEntityEventHandler),
                    new EventHandlerJobInfo<CreatedAccountEvent>(context.MessageId, message, true, null)));
        }
        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);
    }
}
