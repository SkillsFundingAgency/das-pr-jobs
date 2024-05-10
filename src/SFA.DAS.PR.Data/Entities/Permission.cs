namespace SFA.DAS.PR.Data.Entities;

public class Permission
{
    public long Id { get; set; }
    public long AccountProviderLegalEntityId { get; set; }
    public Operation Operation { get; set; }
    public virtual AccountProviderLegalEntity AccountProviderLegalEntity { get; set; } = null!;
}
