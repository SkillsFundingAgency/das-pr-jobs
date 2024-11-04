using WireMock.Logging;
using WireMock.Net.StandAlone;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Settings;

namespace SFA.DAS.PR.Jobs.MockServer;

internal class RecruitApiMockServer
{
    public static void Run()
    {
        var settings = new WireMockServerSettings
        {
            Port = 7054,
            UseSSL = true,
            Logger = new WireMockConsoleLogger()
        };

        var server = StandAloneApp.Start(settings);

        server
            .Given(Request.Create().WithPath("/api/LiveVacancies/*")
            .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBodyFromFile("Data/live-vacancy.json"));
    }
}
