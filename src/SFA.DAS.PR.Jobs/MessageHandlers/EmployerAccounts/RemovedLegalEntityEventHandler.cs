using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;

public record PermissionAuditDetails(long accountId, long accountLegalEntityId, long Ukprn, IEnumerable<Operation> operations);

public class RemovedLegalEntityEventHandler(IProviderRelationshipsDataContext _providerRelationshipsDataContext, IPasAccountApiClient _pasAccountApiClient, ILogger<RemovedLegalEntityEventHandler> _logger) : IHandleMessages<RemovedLegalEntityEvent>
{
    private const string TemplateId = "DeletedPermissionsEventNotification";

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

            IEnumerable<JobAudit> jobAudits = CreateJobAudits(context.MessageId, auditDetails);

            await _providerRelationshipsDataContext.JobAudits.AddRangeAsync(jobAudits, context.CancellationToken);

            await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);
        }
    }
    
    private IEnumerable<JobAudit> CreateJobAudits(string messageId, IEnumerable<PermissionAuditDetails> auditDetails)
    {
        return auditDetails.Select(a => new JobAudit(
            nameof(RemovedLegalEntityEventHandler),
            new EventHandlerJobInfo<PermissionAuditDetails>(messageId, a, true, null)
        ));
    }
}
