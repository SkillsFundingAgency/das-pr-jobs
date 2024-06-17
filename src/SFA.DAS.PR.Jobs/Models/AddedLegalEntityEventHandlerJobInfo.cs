using SFA.DAS.EmployerAccounts.Messages.Events;

namespace SFA.DAS.PR.Jobs.Models;
public record AddedLegalEntityEventHandlerJobInfo(string MessageId, AddedLegalEntityEvent Event, bool IsSuccess, string? FailureReason);
