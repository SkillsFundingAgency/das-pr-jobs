using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;

public record PermissionAuditDetails(long accountId, long accountLegalEntityId, long Ukprn, IEnumerable<Operation> operations);

public class RemovedLegalEntityEventHandler(IProviderRelationshipsDataContext _providerRelationshipsDataContext, ILogger<RemovedLegalEntityEventHandler> _logger) : IHandleMessages<RemovedLegalEntityEvent>
{
    public const string RemovedLegalEntityEventFailureReason = "Account legal entity does not exist";

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
            _logger.LogWarning("Legal entity with Id:{LegalEntityId} does not exist", message.LegalEntityId);

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
            IEnumerable<Permission> permissions = accountLegalEntity.AccountProviderLegalEntities.SelectMany(a => a.Permissions);

            IEnumerable<PermissionAuditDetails> auditDetails = accountLegalEntity.AccountProviderLegalEntities.Select(a =>
                new PermissionAuditDetails(
                    message.AccountId,
                    message.AccountLegalEntityId,
                    a.AccountProvider.ProviderUkprn,
                    a.Permissions.Select(a => a.Operation)
                )
            );

            _providerRelationshipsDataContext.Permissions.RemoveRange(permissions);

            _providerRelationshipsDataContext.AccountProviderLegalEntities.RemoveRange(
                accountLegalEntity.AccountProviderLegalEntities
            );
            
            accountLegalEntity.Deleted = message.Created;

            await CreateJobAudits(context.MessageId, auditDetails, context.CancellationToken);

            await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("Permissions removed for {LegalEntityId}", message.LegalEntityId);
        }
    }
    
    private async Task CreateJobAudits(string messageId, IEnumerable<PermissionAuditDetails> auditDetails, CancellationToken cancellationToken)
    {
        IEnumerable<JobAudit> jobAudits = auditDetails.Select(a => new JobAudit(
            nameof(RemovedLegalEntityEventHandler),
            new EventHandlerJobInfo<PermissionAuditDetails>(messageId, a, true, null)
        ));

        await _providerRelationshipsDataContext.JobAudits.AddRangeAsync(jobAudits, cancellationToken);
    }
}
