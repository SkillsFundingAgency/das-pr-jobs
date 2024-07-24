using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.PR.Data.Extensions;
using SFA.DAS.PR.Jobs.Configuration;
using SFA.DAS.PR.Jobs.Extensions;
using System.Configuration;

[assembly: NServiceBusTriggerFunction("SFA.DAS.PR.Jobs")]

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(builder => builder.AddConfiguration())
    .ConfigureNServiceBus()
    .ConfigureServices((context, services) =>
    {
        services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights()
            .AddPrDataContext(context.Configuration)
            .AddServiceRegistrations(context.Configuration);
    })
    .Build();

host.Run();
