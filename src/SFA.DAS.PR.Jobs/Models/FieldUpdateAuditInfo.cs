namespace SFA.DAS.PR.Jobs.Models;

public record FieldUpdateAuditInfo(string FieldUpdated, string? InitialState, string UpdatedState);
