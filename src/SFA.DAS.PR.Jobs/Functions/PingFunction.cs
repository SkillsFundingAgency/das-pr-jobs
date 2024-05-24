using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.PR.Data.Repositories;

namespace SFA.DAS.PR.Jobs.Functions;

public class PingFunction(ILogger<PingFunction> _logger, IProvidersRepository _providersRepository, IFunctionEndpoint _functionEndpoint)
{
    [Function("ping")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, FunctionContext executionContext, CancellationToken cancellationToken)
    {
        await _functionEndpoint.Publish(new HelloWorldEvent(executionContext.FunctionId), executionContext);

        var providersCount = await _providersRepository.GetCount(cancellationToken);

        if (providersCount == 0) _logger.LogWarning("Providers cache is empty");

        var str = $"You have successfully invoked Ping function in Provider Relationships Jobs! at {DateTime.UtcNow}. The current count of providers is {providersCount}";
        _logger.LogInformation(str);

        return new OkObjectResult(new { executionContext.FunctionId, providersCount, DateTime.UtcNow });
    }
}

public record HelloWorldEvent(string FunctionId);
