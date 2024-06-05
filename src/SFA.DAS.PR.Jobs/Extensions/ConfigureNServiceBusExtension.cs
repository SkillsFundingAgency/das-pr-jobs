using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;

namespace SFA.DAS.PR.Jobs.Extensions;

public static class ConfigureNServiceBusExtension
{
    const string ErrorEndpointName = $"SFA.DAS.PR.Jobs-error";

    public static IHostBuilder ConfigureNServiceBus(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseNServiceBus((configuration, endpointConfiguration) =>
        {
            endpointConfiguration.AdvancedConfiguration.EnableInstallers();

            endpointConfiguration.AdvancedConfiguration.Conventions()
                .DefiningCommandsAs(t => Regex.IsMatch(t.Name, "Command(V\\d+)?$"))
                .DefiningEventsAs(t => Regex.IsMatch(t.Name, "Event(V\\d+)?$"));

            endpointConfiguration.AdvancedConfiguration.SendFailedMessagesTo(ErrorEndpointName);

            var persistence = endpointConfiguration.AdvancedConfiguration.UsePersistence<AzureTablePersistence>();
            persistence.ConnectionString(configuration["AzureWebJobsStorage"]);

            var decodedLicence = WebUtility.HtmlDecode(configuration["NServiceBusConfiguration:NServiceBusLicense"]);
            endpointConfiguration.AdvancedConfiguration.License(decodedLicence);
        });
        return hostBuilder;
    }
}
