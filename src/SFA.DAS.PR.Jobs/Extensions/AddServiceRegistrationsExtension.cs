using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using SFA.DAS.PR.Jobs.Configuration;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Services;

namespace SFA.DAS.PR.Jobs.Extensions;

[ExcludeFromCodeCoverage]
public static class AddServiceRegistrationsExtension
{
    public static IServiceCollection AddServiceRegistrations(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .RegisterServices()
            .AddHttpClient()
            .RegisterRoatpServiceApiClient(configuration)
            .RegisterRecruitServiceApiClient(configuration)
            .RegisterPasAccountApiClient(configuration)
            .RegisterCommitmentsV2ApiClient(configuration)
            .BindConfiguration(configuration);

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

    private static IServiceCollection RegisterRecruitServiceApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        var recruitServiceApiConfiguration = configuration.GetSection("RecruitApiConfiguration").Get<InnerApiConfiguration>()!;

        services.AddRefitClient<IRecruitApiClient>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(recruitServiceApiConfiguration.Url))
                .AddHttpMessageHandler(() => new InnerApiAuthenticationHeaderHandler(new AzureClientCredentialHelper(), recruitServiceApiConfiguration.Identifier));

        return services;
    }

    private static IServiceCollection RegisterCommitmentsV2ApiClient(this IServiceCollection services,
        IConfiguration configuration)
    {
        var commitmentsV2ApiClientConfiguration = configuration.GetSection("CommitmentsV2ApiConfiguration")
            .Get<InnerApiConfiguration>()!;

        services.AddRefitClient<ICommitmentsV2ApiClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(commitmentsV2ApiClientConfiguration.Url))
            .AddHttpMessageHandler(() => new InnerApiAuthenticationHeaderHandler(new AzureClientCredentialHelper(),
                commitmentsV2ApiClientConfiguration.Identifier));

        return services;
    }

    private static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddTransient<INotificationTokenService, NotificationTokenService>();

        return services;
    }

    private static IServiceCollection BindConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NotificationsConfiguration>(configuration.GetSection("ApplicationConfiguration:Notifications"));

        return services;
    }
}
