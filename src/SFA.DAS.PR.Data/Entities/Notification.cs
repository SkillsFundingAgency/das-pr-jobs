namespace SFA.DAS.PR.Data.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public required string TemplateName { get; set; }
    public required string NotificationType { get; set; }
    public long? Ukprn { get; set; }
    public string? EmailAddress { get; set; }
    public string? Contact { get; set; }
    public string? EmployerName { get; set; }
    public Guid? RequestId { get; set; }
    public long? AccountLegalEntityId { get; set; }
    public short? PermitApprovals { get; set; }
    public short? PermitRecruit { get; set; }
    public required string CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? SentTime { get; set; }
}
