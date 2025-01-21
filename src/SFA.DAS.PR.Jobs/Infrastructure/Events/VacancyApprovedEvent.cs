namespace Esfa.Recruit.Vacancies.Client.Domain.Events;
public class VacancyApprovedEvent
{
    public required string AccountLegalEntityPublicHashedId { get; set; }
    public long Ukprn { get; set; }
    public Guid VacancyId { get; set; }
    public long VacancyReference { get; set; }
}