namespace SFA.DAS.PR.Data.Entities;

public sealed class PermissionAudit
{
    public Guid Id { get; set; }
    public required DateTime Eventtime { get; set; }
    public required string Action { get; set; }
    public required long Ukprn { get; set; }
    public required long AccountLegalEntityId { get; set; }
    public Guid? EmployerUserRef { get; set; }
    public required string Operations { get; set; }
}

public enum PermissionAction : short
{
    ApprovalsRelationship,
    RecruitRelationship,
    PermissionCreated,
    PermissionUpdated,
    PermissionDeleted,
    AccountCreated,
    AccountAdded
}