using Microsoft.Extensions.Logging;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Data;

namespace SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;

public class RemovedLegalEntityEventHandler(IProviderRelationshipsDataContext _providerRelationshipsDataContext, ILogger<RemovedLegalEntityEventHandler> _logger) : IHandleMessages<CreatedAccountEvent>
{
    public const string RemovedLegalEntityEventFailureReason = "Account already exists";
    public async Task Handle(CreatedAccountEvent message, IMessageHandlerContext context)
    {
        // new EventHandlerJobInfo<CreatedAccountEvent>(context.MessageId, message, false, AccountAlreadyExistsFailureReason)

        // new(nameof(AddedLegalEntityEventHandler),
        // new EventHandlerJobInfo<CreatedAccountEvent>(context.MessageId, message, true, null))

        // Validation: Account legal entity should exist, otherwise just create audit record with failure reason. 

        // We need to do the following :

        // Delete all the AccountProviderLegalEntities records associated with this account legal entity

        // Set Deleted on AccountLegalEntity
        // Delete all the permissions record associated with this account legal entity

        // For all the permissions that are being deleted, we need to send out notifications.In the current implementation this is done via raising DeletedPermissionsEventV2 event, however we are not going to use events for internal changes, so the logic that is part of DeletedPermissionsEventV2Handler should be included in here.

        // Create audit record for each permission deleted.Audit info should capture, AccountId, AccountLegalEntityId, Ukprn, Existing Permissions

        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);
    }
}
