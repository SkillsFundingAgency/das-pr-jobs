using Refit;
using SFA.DAS.PR.Jobs.Models.Recruit;

namespace SFA.DAS.PR.Jobs.Infrastructure;

public interface IRecruitApiClient
{
    [Get("/api/LiveVacancies/{vacancyReference}")]
    Task<GetLiveVacancyQueryResponse> GetLiveVacancy([AliasAs("vacancyReference")]long vacancyReference, CancellationToken cancellationToken);
}
