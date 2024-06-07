using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.PR.Jobs.Infrastructure;

namespace SFA.DAS.PR.Jobs.Functions;

public class UpdateProvidersFunction(ILogger<UpdateProvidersFunction> _logger, IRoatpServiceApiClient _roatpClient)
{
    [Function(nameof(UpdateProvidersFunction))]
    public async Task Run([TimerTrigger("%UpdateProvidersFunctionSchedule%", RunOnStartup = false)] TimerInfo myTimer, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateProvidersFunction executed at: {TimeStamp}", DateTime.UtcNow);

        var providers = await _roatpClient.GetProviders(cancellationToken);

        _logger.LogInformation("Total {NumberOfProviders} registered providers ", providers.Count);
    }
}
