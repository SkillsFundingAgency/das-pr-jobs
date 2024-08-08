namespace SFA.DAS.PR.Jobs.Configuration;

public class NotificationsConfiguration
{
    public string ProviderPortalUrl { get; set; } = null!;
    public int BatchSize { get; set; }
    public List<TemplateConfiguration> NotificationTemplates { get; set; } = null!;
}
