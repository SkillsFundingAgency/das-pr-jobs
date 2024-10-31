using Refit;
using SFA.DAS.PR.Jobs.OuterApi.Responses;

namespace SFA.DAS.PR.Jobs.Infrastructure;
public interface ICommitmentsV2ApiClient
{
    [Get("/api/cohorts/{cohortId}")]
    Task<CohortModel> GetCohortDetails(long cohortId, CancellationToken cancellationToken);
}
