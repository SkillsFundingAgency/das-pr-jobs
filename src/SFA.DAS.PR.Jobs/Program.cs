using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.PR.Data.Extensions;
using SFA.DAS.PR.Jobs.Extensions;

[assembly: NServiceBusTriggerFunction("SFA.DAS.PR.Jobs")]

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .UseNServiceBus()
    .ConfigureAppConfiguration(builder => builder.AddConfiguration())
    .ConfigureServices((context, services) =>
    {
        services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights()
            .AddPrDataContext(context.Configuration);
    })
    .Build();

host.Run();
