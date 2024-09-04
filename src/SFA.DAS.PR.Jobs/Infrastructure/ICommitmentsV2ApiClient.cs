using Refit;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Jobs.Infrastructure;
public interface ICommitmentsV2ApiClient
{
    [Get("/api/cohorts/{cohortId}")]
    Task<Cohort> GetCohortDetails(long cohortId, CancellationToken cancellationToken);
}
