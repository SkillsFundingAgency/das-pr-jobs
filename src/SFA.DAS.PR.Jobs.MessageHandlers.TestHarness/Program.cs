using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.PR.Jobs.MessageHandlers.TestHarness;
using static SFA.DAS.PR.Jobs.MessageHandlers.TestHarness.ConfigureNServiceBusExtension;

await Host
    .CreateDefaultBuilder()
    .ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddJsonFile("local.settings.json"))
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<RaiseEventService>();

        const string endpointName = "SFA.DAS.PR.Jobs.TestHarness";
        var endpointConfiguration = new EndpointConfiguration(endpointName);

        var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
        transport.ConnectionString(context.Configuration["AzureWebJobsServiceBus"]);

        transport.SubscriptionRuleNamingConvention(AzureRuleNameShortener.Shorten);

        endpointConfiguration.UsePersistence<LearningPersistence>();
        endpointConfiguration.SendOnly();
        endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();
        endpointConfiguration.Conventions()
            .DefiningCommandsAs(t => Regex.IsMatch(t.Name, "Command(V\\d+)?$"))
            .DefiningEventsAs(t => Regex.IsMatch(t.Name, "Event(V\\d+)?$"));

        var endpointInstance = Endpoint.Start(endpointConfiguration).GetAwaiter().GetResult();
        services.AddSingleton(endpointInstance);
        services.AddSingleton<IMessageSession>(endpointInstance);
    })
    .Build()
    .RunAsync();
