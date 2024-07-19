namespace SFA.DAS.PR.Jobs.Configuration;

public class NotificationsConfiguration
{
    public int BatchSize { get; set; }
    public List<TemplateConfiguration> Templates { get; set; } = null!;
}
