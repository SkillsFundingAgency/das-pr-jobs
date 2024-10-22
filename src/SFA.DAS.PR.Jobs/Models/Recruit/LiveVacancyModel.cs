namespace SFA.DAS.PR.Jobs.Models.Recruit;

public class LiveVacancy
{
    public Guid VacancyId { get; set; }
    public TrainingProviderModel TrainingProvider { get; set; }
    public string AccountPublicHashedId { get; set; }
    public string AccountLegalEntityPublicHashedId { get; set; }
}

public class TrainingProviderModel
{
    public long? Ukprn { get; set; }
}
