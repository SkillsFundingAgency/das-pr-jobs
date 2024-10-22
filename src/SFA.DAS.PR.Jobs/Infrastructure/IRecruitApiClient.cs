using Refit;
using SFA.DAS.PAS.Account.Api.Types;
using SFA.DAS.PR.Jobs.Models.Recruit;

namespace SFA.DAS.PR.Jobs.Infrastructure;

public interface IRecruitApiClient
{
    [Get("/api/LiveVacancies/{vacancyReference}")]
    Task<GetLiveVacancyQueryResponse> GetLiveVacancies([AliasAs("vacancyReference")]long vacancyReference, CancellationToken cancellationToken);
}
