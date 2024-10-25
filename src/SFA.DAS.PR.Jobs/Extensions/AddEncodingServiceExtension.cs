using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Encoding;
using SFA.DAS.PR.Jobs.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SFA.DAS.PR.Jobs.Extensions;

[ExcludeFromCodeCoverage]
public static class AddEncodingServiceExtension
{
    public static IServiceCollection AddEncodingService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEncodingConfiguration(configuration);

        services.AddSingleton<IEncodingService, EncodingService>();
        return services;
    }

    private static IServiceCollection AddEncodingConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var encodingsConfiguration = configuration.GetSection(ConfigurationKeys.EncodingConfig).Value;
        var encodingConfig = JsonSerializer.Deserialize<EncodingConfig>(encodingsConfiguration!);
        services.AddSingleton(encodingConfig!);

        return services;
    }
}