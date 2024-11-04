using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface IPermissionAuditRepository
{
    Task CreatePermissionAudit(PermissionsAudit permissionAudit, CancellationToken cancellationToken);
}

public sealed class PermissionAuditRepository(IProviderRelationshipsDataContext providerRelationshipsDataContext) : IPermissionAuditRepository
{
    public async Task CreatePermissionAudit(PermissionsAudit permissionAudit, CancellationToken cancellationToken)
    {
        await providerRelationshipsDataContext.PermissionsAudit.AddAsync(permissionAudit, cancellationToken);
    }
}
