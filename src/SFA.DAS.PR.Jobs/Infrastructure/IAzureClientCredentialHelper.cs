namespace SFA.DAS.PR.Jobs.Infrastructure;

public interface IAzureClientCredentialHelper
{
    Task<string> GetAccessTokenAsync(string identifier);
}
