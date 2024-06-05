namespace SFA.DAS.PR.Data.Entities;

public class Provider
{
    public long Ukprn { get; set; }
    public string Name { get; set; } = null!;
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }

    public virtual List<AccountProvider> AccountProviders { get; set; } = new();
}
