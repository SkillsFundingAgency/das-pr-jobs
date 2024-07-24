using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Services;
using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.PR.Jobs.Extensions;

[ExcludeFromCodeCoverage]
public static class AddServiceRegistrationsExtension
{
    public static IServiceCollection AddServiceRegistrations(this IServiceCollection services, IConfiguration configuration)
    {
        
        services
            .RegisterServices()
            .RegisterRoatpServiceApiClient(configuration)
            .RegisterPasAccountApiClient(configuration)
            .AddHttpClient();
            
        return services;
    }

    private static IServiceCollection RegisterPasAccountApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        var pasAccountApiClientConfiguration = configuration.GetSection("PasAccountApiConfiguration").Get<InnerApiConfiguration>()!;

        services.AddRefitClient<IPasAccountApiClient>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(pasAccountApiClientConfiguration.Url))
                .AddHttpMessageHandler(() => new InnerApiAuthenticationHeaderHandler(new AzureClientCredentialHelper(), pasAccountApiClientConfiguration.Identifier));

        return services;
    }
    private static IServiceCollection RegisterRoatpServiceApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        var roatpServiceApiConfiguration = configuration.GetSection("RoatpServiceApiConfiguration").Get<InnerApiConfiguration>()!;

        services.AddRefitClient<IRoatpServiceApiClient>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(roatpServiceApiConfiguration.Url))
                .AddHttpMessageHandler(() => new InnerApiAuthenticationHeaderHandler(new AzureClientCredentialHelper(), roatpServiceApiConfiguration.Identifier));

        return services;
    }

    private static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddTransient<ITokenService, TokenService>();

        return services;
    }
}
