using Refit;
using SFA.DAS.Recruit.Vacancies.Client.Entities;

namespace SFA.DAS.PR.Jobs.Infrastructure;

public interface IRecruitApiClient
{
    [Get("/api/LiveVacancies/{vacancyReference}")]
    Task<Vacancy> GetLiveVacancy(long vacancyReference, CancellationToken cancellationToken);
}
