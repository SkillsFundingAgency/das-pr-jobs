namespace SFA.DAS.PR.Jobs.Models.Recruit;

public sealed class GetLiveVacancyQueryResponse
{
    public ResponseCode ResultCode { get; set; }
    public List<object> ValidationErrors { get; set; } = new List<object>();
    public LiveVacancyModel? LiveVacancy { get; set; }
}
