using Refit;
using SFA.DAS.PR.Jobs.Models.Recruit;

namespace SFA.DAS.PR.Jobs.Infrastructure;

public interface IRecruitApiClient
{
    [Get("/api/LiveVacancies/{vacancyReference}")]
    Task<LiveVacancyModel> GetLiveVacancy(long vacancyReference, CancellationToken cancellationToken);
}
