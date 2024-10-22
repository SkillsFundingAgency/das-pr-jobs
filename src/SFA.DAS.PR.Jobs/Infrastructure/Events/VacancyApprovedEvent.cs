namespace Esfa.Recruit.Vacancies.Client.Domain.Events;
public class VacancyApprovedEvent
{
    public Guid VacancyId { get; set; }
    public long VacancyReference { get; set; }
}
