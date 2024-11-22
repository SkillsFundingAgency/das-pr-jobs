namespace SFA.DAS.PR.Jobs.OuterApi.Responses;

public sealed class AccountDetails
{
    public long AccountId { get; set; }
    public required string HashedAccountId { get; set; }
    public required string PublicHashedAccountId { get; set; }
    public required string DasAccountName { get; set; }
}
