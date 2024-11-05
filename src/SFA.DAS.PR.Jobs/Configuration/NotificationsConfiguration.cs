namespace SFA.DAS.PR.Jobs.Configuration;

public class NotificationsConfiguration
{
    public string ProviderPortalUrl { get; set; } = null!;
    public int BatchSize { get; set; }
    public int RequestExpiry { get; set; }
    public int NotificationRetentionDays { get; set; }
    public string EmployerPRBaseUrl { get; set; } = null!;
    public string EmployerAccountsBaseUrl { get; set; } = null!;
    public string ProviderPRBaseUrl { get; set; } = null!;
    public List<TemplateConfiguration> NotificationTemplates { get; set; } = null!;
}
