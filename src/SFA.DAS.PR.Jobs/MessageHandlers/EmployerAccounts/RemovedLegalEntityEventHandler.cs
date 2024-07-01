using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;

//public class SendDeletedPermissionsNotificationCommand : IRequest
//{
//    public long Ukprn { get; set; }
//    public long AccountLegalEntityId { get; set; }

//    public SendDeletedPermissionsNotificationCommand
//    (
//        long ukprn,
//        long accountLegalEntityId
//    )
//    {
//        Ukprn = ukprn;
//        AccountLegalEntityId = accountLegalEntityId;
//    }
//}

public record PermissionAuditDetails(long accountId, long accountLegalEntityId, long Ukprn, IEnumerable<Operation> operations);

public class RemovedLegalEntityEventHandler(IProviderRelationshipsDataContext _providerRelationshipsDataContext, ILogger<RemovedLegalEntityEventHandler> _logger) : IHandleMessages<RemovedLegalEntityEvent>
{
    public const string RemovedLegalEntityEventFailureReason = "Account legal entity does not exists";

    public async Task Handle(RemovedLegalEntityEvent message, IMessageHandlerContext context)
    {
        AccountLegalEntity? accountLegalEntity = await _providerRelationshipsDataContext.AccountLegalEntities
            .Include(a => a.AccountProviderLegalEntities)
                .ThenInclude(b => b.Permissions)
            .Include(a => a.AccountProviderLegalEntities)
                .ThenInclude(b => b.AccountProvider)
        .FirstOrDefaultAsync(a => a.Id == message.AccountLegalEntityId, context.CancellationToken);

        if (accountLegalEntity is null)
        {
            await _providerRelationshipsDataContext.JobAudits.AddAsync(
                new(
                    nameof(RemovedLegalEntityEventHandler),
                    new EventHandlerJobInfo<RemovedLegalEntityEvent>(context.MessageId, message, false, RemovedLegalEntityEventFailureReason)
                ),
                context.CancellationToken
            );

            await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);
        }
        else
        {
            // For all the permissions that are being deleted, we need to send out notifications. In the current implementation this is
            // done via raising DeletedPermissionsEventV2 event, however we are not going to use events for internal changes,
            // so the logic that is part of DeletedPermissionsEventV2Handler should be included in here.

            IEnumerable<Permission> permissions = accountLegalEntity.AccountProviderLegalEntities.SelectMany(a => a.Permissions);

            // Create audit record for each permission deleted.
            // Audit info should capture: AccountId, AccountLegalEntityId, Ukprn, Existing Permissions:

            IEnumerable<PermissionAuditDetails> auditDetails = accountLegalEntity.AccountProviderLegalEntities.Select(a =>
                new PermissionAuditDetails(
                    message.AccountId,
                    message.AccountLegalEntityId,
                    a.AccountProvider.ProviderUkprn,
                    a.Permissions.SelectMany(a => a.Operation) //  operations
                )
            ); // Single

            // Delete all the permissions record associated with this account legal entity.

            _providerRelationshipsDataContext.Permissions.RemoveRange(permissions);

            // Delete all the AccountProviderLegalEntities records associated with this account legal entity.
            
            _providerRelationshipsDataContext.AccountProviderLegalEntities.RemoveRange(
                accountLegalEntity.AccountProviderLegalEntities
            );
            
            accountLegalEntity.Deleted = message.Created;
            
            // Create audit record for each permission deleted.
            //jobAudit = new(
            //    nameof(RemovedLegalEntityEventHandler),
            //    new EventHandlerJobInfo<RemovedLegalEntityEvent>(context.MessageId, message, true, null)
            //);
            //await _providerRelationshipsDataContext.JobAudits.AddRangeAsync(//JobAudits, context.CancellationToken);

            await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);
        }

        // Set Deleted on AccountLegalEntityt

    }
}
