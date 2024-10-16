using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.Repositories;

public interface IJobAuditRepository
{
    Task CreateJobAudit(JobAudit jobAudit, CancellationToken cancellationToken);
}

public sealed class JobAuditRepository(IProviderRelationshipsDataContext _providerRelationshipsDataContext) : IJobAuditRepository
{
    public async Task CreateJobAudit(JobAudit jobAudit, CancellationToken cancellationToken)
    {
        await _providerRelationshipsDataContext.JobAudits.AddAsync(jobAudit, cancellationToken);
    }
}
