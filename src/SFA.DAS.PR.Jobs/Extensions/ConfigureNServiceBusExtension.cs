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
            endpointConfiguration.AdvancedConfiguration.AssemblyScanner().ScanFileSystemAssemblies = false;
            endpointConfiguration.AdvancedConfiguration.CustomDiagnosticsWriter((diagnostics, _) =>
            {
                Console.WriteLine(diagnostics);
                return Task.CompletedTask;
            });
            endpointConfiguration.Transport.SubscriptionRuleNamingConvention = AzureRuleNameShortener.Shorten;

            endpointConfiguration.AdvancedConfiguration.EnableInstallers();
            endpointConfiguration.AdvancedConfiguration.SendFailedMessagesTo(ErrorEndpointName);
            endpointConfiguration.AdvancedConfiguration.Conventions()
                .DefiningCommandsAs(t => Regex.IsMatch(t.Name, "Command(V\\d+)?$"))
                .DefiningEventsAs(t => Regex.IsMatch(t.Name, "Event(V\\d+)?$"));
            endpointConfiguration.Routing.RouteToEndpoint(typeof(SendEmailCommand), NotificationsQueue);

            var value = configuration["NServiceBusConfiguration:NServiceBusLicense"];
            if (!string.IsNullOrEmpty(value))
            {
                var decodedLicence = WebUtility.HtmlDecode(value);
                endpointConfiguration.AdvancedConfiguration.License(decodedLicence);
            }
        });
        return hostBuilder;
    }
}
