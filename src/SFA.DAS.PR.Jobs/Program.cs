using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.PR.Data.Extensions;
using SFA.DAS.PR.Jobs.Extensions;

[assembly: NServiceBusTriggerFunction("SFA.DAS.PR.Jobs")]

const string ErrorEndpointName = $"SFA.DAS.PR.Jobs-error";

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(builder => builder.AddConfiguration())
    .UseNServiceBus((configuration, endpointConfiguration) =>
    {
        endpointConfiguration.AdvancedConfiguration.EnableInstallers();

        endpointConfiguration.AdvancedConfiguration.Conventions()
            .DefiningCommandsAs(t => Regex.IsMatch(t.Name, "Command(V\\d+)?$"))
            .DefiningEventsAs(t => Regex.IsMatch(t.Name, "Event(V\\d+)?$"));

        endpointConfiguration.AdvancedConfiguration.SendFailedMessagesTo(ErrorEndpointName);

        var persistence = endpointConfiguration.AdvancedConfiguration.UsePersistence<AzureTablePersistence>();
        persistence.ConnectionString(configuration["AzureWebJobsStorage"]);

        endpointConfiguration.AdvancedConfiguration.License(configuration["NServiceBusConfiguration:NServiceBusLicense"]);
    })
    .ConfigureServices((context, services) =>
    {
        services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights()
            .AddPrDataContext(context.Configuration);
    })
    .Build();

host.Run();
