namespace SFA.DAS.PR.Jobs.Configuration;

public interface INotificationsConfiguration
{
    int BatchSize { get; set; }
    List<TemplateConfiguration> NotificationTemplates { get; set; }
}

public class NotificationsConfiguration : INotificationsConfiguration
{
    public int BatchSize { get; set; }
    public List<TemplateConfiguration> NotificationTemplates { get; set; } = null!;
}
