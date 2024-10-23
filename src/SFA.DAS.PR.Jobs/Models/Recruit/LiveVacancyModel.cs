namespace SFA.DAS.PR.Jobs.Models.Recruit;

public class LiveVacancyModel
{
    public required Guid VacancyId { get; set; }
    public TrainingProviderModel? TrainingProvider { get; set; }
    public required string AccountPublicHashedId { get; set; }
    public required string AccountLegalEntityPublicHashedId { get; set; }
}

public class TrainingProviderModel
{
    public long Ukprn { get; set; }
}
