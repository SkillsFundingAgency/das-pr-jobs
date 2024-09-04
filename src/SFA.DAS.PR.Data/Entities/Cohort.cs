namespace SFA.DAS.PR.Data.Entities;
public class Cohort
{
    public long CohortId { get; set; }
    public long AccountId { get; set; }
    public long AccountLegalEntityId { get; set; }
    public string LegalEntityName { get; set; }
    public string ProviderName { get; set; }
    public long ProviderId { get; set; }
}
