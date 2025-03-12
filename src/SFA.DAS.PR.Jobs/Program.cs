using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    .ConfigureLogging(logging =>
    {
        // This rule filters logs to capture only warnings and errors, removing this rule will allow Information logs to be captured
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            LoggerFilterRule? defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
                == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (defaultRule is not null)
            {
                options.Rules.Remove(defaultRule);
            }
        });
    })
    .Build();

host.Run();
