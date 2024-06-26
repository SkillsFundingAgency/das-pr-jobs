using Microsoft.Extensions.Logging;
using SFA.DAS.PR.Jobs.Events;

namespace SFA.DAS.PR.Jobs.MessageHandlers;

public class HelloWorldEventHandler(ILogger<HelloWorldEventHandler> _logger) : IHandleMessages<HelloWorldEvent>
{
    public Task Handle(HelloWorldEvent message, IMessageHandlerContext context)
    {
        _logger.LogWarning("Handling Hello world event raised by function id: {FunctionId} at: {Time}", message.FunctionId, DateTime.UtcNow.ToString());
        return Task.CompletedTask;
    }
}
