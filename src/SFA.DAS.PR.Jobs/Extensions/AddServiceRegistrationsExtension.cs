using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Services;

namespace SFA.DAS.PR.Jobs.Extensions;

[ExcludeFromCodeCoverage]
public static class AddServiceRegistrationsExtension
{
    public static IServiceCollection AddServiceRegistrations(this IServiceCollection services, IConfiguration configuration)
    {
        var roatpServiceApiConfiguration = configuration.GetSection("RoatpServiceApiConfiguration").Get<InnerApiConfiguration>()!;
        services
            .RegisterServices()
            .AddHttpClient()
            .AddRefitClient<IRoatpServiceApiClient>()
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
