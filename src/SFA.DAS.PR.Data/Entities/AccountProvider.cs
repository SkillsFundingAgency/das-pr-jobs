namespace SFA.DAS.PR.Data.Entities;

public class AccountProvider
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public long ProviderUkprn { get; set; }
    public DateTime Created { get; set; }
    public virtual Account Account { get; set; } = null!;
    public virtual Provider Provider { get; set; } = null!;
    public virtual List<AccountProviderLegalEntity> AccountProviderLegalEntities { get; set; } = [];
}
