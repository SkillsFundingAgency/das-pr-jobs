namespace SFA.DAS.PR.Jobs.OuterApi.Requests;

public class RemovePermissionsRequest
{
    public Guid UserRef { get; set; }
    public int? Ukprn { get; set; }
    public long AccountLegalEntityId { get; set; }
}
