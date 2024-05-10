namespace SFA.DAS.PR.Data.Entities;

public class AccountProviderLegalEntity
{
    public long Id { get; set; }
    public long AccountProviderId { get; set; }
    public long AccountLegalEntityId { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
    public virtual AccountLegalEntity AccountLegalEntity { get; set; } = null!;
    public virtual AccountProvider AccountProvider { get; set; } = null!;
    public virtual List<Permission> Permissions { get; set; } = new();
}
