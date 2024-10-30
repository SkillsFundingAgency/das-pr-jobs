namespace SFA.DAS.PR.Jobs.OuterApi.Responses;
public class CohortModel
{
    public long CohortId { get; set; }
    public long AccountId { get; set; }
    public long AccountLegalEntityId { get; set; }
    public required string LegalEntityName { get; set; }
    public required string ProviderName { get; set; }
    public long ProviderId { get; set; }
}
