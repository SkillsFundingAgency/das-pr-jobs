using WireMock.Logging;
using WireMock.Net.StandAlone;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace SFA.DAS.PR.Jobs.MockServer;
internal static class CommitmentsV2ApiMockServer
{
    public static WireMockServer Run()
    {
        var settings = new WireMockServerSettings
        {
            Port = 5011,
            UseSSL = true,
            Logger = new WireMockConsoleLogger()
        };

        var server = StandAloneApp.Start(settings);

        server
            .Given(Request.Create().WithPath("/api/cohorts/123")
            .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Data/cohort-details.json"));

        return server;
    }
}
