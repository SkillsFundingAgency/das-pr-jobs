using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.PR.Data.Extensions;
using SFA.DAS.PR.Jobs.Extensions;

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
            .AddEncodingService(context.Configuration)
            .AddServiceRegistrations(context.Configuration);
    })
    .Build();

host.Run();
