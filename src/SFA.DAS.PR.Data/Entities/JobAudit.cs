namespace SFA.DAS.PR.Data.Entities;
public class JobAudit
{
    public long Id { get; set; }
    public string JobName { get; set; } = null!;
    public string? JobInfo { get; set; }
}
