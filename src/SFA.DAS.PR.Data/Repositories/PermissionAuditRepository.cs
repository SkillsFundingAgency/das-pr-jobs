using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface IPermissionAuditRepository
{
    Task CreatePermissionAudit(PermissionAudit permissionAudit, CancellationToken cancellationToken);
}

public sealed class PermissionAuditRepository(IProviderRelationshipsDataContext providerRelationshipsDataContext) : IPermissionAuditRepository
{
    public async Task CreatePermissionAudit(PermissionAudit permissionAudit, CancellationToken cancellationToken)
    {
        await providerRelationshipsDataContext.PermissionAudits.AddAsync(permissionAudit, cancellationToken);
    }
}
