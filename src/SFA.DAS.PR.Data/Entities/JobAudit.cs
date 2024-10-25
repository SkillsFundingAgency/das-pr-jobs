using System.Text.Json;

namespace SFA.DAS.PR.Data.Entities;

public class JobAudit
{
    private static readonly JsonSerializerOptions options = new() { WriteIndented = true };
    public long Id { get; set; }
    public string JobName { get; set; } = null!;
    public string? JobInfo { get; set; }
    public DateTime ExecutedOn { get; set; }
    public JobAudit() { }
    public JobAudit(string jobName, object jobInfo)
    {
        JobName = jobName;
        JobInfo = JsonSerializer.Serialize(jobInfo, options);
    }
}
