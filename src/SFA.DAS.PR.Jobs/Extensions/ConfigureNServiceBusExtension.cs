using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using SFA.DAS.Notifications.Messages.Commands;

namespace SFA.DAS.PR.Jobs.Extensions;

public static partial class ConfigureNServiceBusExtension
{
    const string ErrorEndpointName = "SFA.DAS.PR.Jobs-error";
    const string NotificationsQueue = "SFA.DAS.Notifications.MessageHandlers";

    public static IHostBuilder ConfigureNServiceBus(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseNServiceBus((configuration, endpointConfiguration) =>
        {
            endpointConfiguration.Transport.SubscriptionRuleNamingConvention = AzureRuleNameShortener.Shorten;

            endpointConfiguration.AdvancedConfiguration.EnableInstallers();

            endpointConfiguration.AdvancedConfiguration.Conventions()
                .DefiningCommandsAs(t => Regex.IsMatch(t.Name, "Command(V\\d+)?$"))
                .DefiningEventsAs(t => Regex.IsMatch(t.Name, "Event(V\\d+)?$"));

            endpointConfiguration.Routing.RouteToEndpoint(typeof(SendEmailCommand), NotificationsQueue);

            endpointConfiguration.AdvancedConfiguration.SendFailedMessagesTo(ErrorEndpointName);

            var persistence = endpointConfiguration.AdvancedConfiguration.UsePersistence<AzureTablePersistence>();
            persistence.ConnectionString(configuration["AzureWebJobsStorage"]);

            var decodedLicense = WebUtility.HtmlDecode(configuration["NServiceBusConfiguration:NServiceBusLicense"]);
            endpointConfiguration.AdvancedConfiguration.License(decodedLicense);

            endpointConfiguration.LogDiagnostics();
        });
        return hostBuilder;
    }
}
