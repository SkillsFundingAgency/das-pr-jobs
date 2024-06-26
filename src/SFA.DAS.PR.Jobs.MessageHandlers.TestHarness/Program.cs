using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.PR.Jobs.MessageHandlers.TestHarness;

await Host
    .CreateDefaultBuilder()
    .ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddJsonFile("local.settings.json"))
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<RaiseEventService>();

        const string endpointName = "SFA.DAS.PR.Jobs.TestHarness";
        var endpointConfiguration = new EndpointConfiguration(endpointName);

        endpointConfiguration.UseTransport(new AzureServiceBusTransport(context.Configuration["AzureWebJobsServiceBus"]));

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
