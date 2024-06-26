﻿using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;

namespace SFA.DAS.PR.Jobs.Infrastructure;

[ExcludeFromCodeCoverage]
public class AzureClientCredentialHelper : IAzureClientCredentialHelper
{
    private const int MaxRetries = 2;
    private readonly TimeSpan _networkTimeout = TimeSpan.FromSeconds(1);
    private readonly TimeSpan _delay = TimeSpan.FromMilliseconds(100);

    public async Task<string> GetAccessTokenAsync(string identifier)
    {
        var azureServiceTokenProvider = new ChainedTokenCredential(
                new ManagedIdentityCredential(options: new TokenCredentialOptions
                {
                    Retry = { NetworkTimeout = _networkTimeout, MaxRetries = MaxRetries, Delay = _delay }
                }),
                new AzureCliCredential(options: new AzureCliCredentialOptions
                {
                    Retry = { NetworkTimeout = _networkTimeout, MaxRetries = MaxRetries, Delay = _delay }
                }),
                new VisualStudioCredential(options: new VisualStudioCredentialOptions
                {
                    Retry = { NetworkTimeout = _networkTimeout, MaxRetries = MaxRetries, Delay = _delay }
                }),
                new VisualStudioCodeCredential(options: new VisualStudioCodeCredentialOptions()
                {
                    Retry = { NetworkTimeout = _networkTimeout, MaxRetries = MaxRetries, Delay = _delay }
                }));

        var accessToken = await azureServiceTokenProvider.GetTokenAsync(new TokenRequestContext(scopes: new[] { identifier }));

        return accessToken.Token;
    }
}
