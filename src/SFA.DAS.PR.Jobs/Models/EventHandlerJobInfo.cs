namespace SFA.DAS.PR.Jobs.Models;
public record EventHandlerJobInfo<T>(string MessageId, T Event, bool IsSuccess, string? FailureReason);
