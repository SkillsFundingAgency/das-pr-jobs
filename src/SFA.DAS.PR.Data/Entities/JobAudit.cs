namespace SFA.DAS.PR.Data.Entities;
public class JobAudit
{
    public long Id { get; set; }
    public required string JobName { get; set; }
    public string? JobInfo { get; set; }
}
