using SFA.DAS.PR.Data.Common;

namespace SFA.DAS.PR.Jobs.Models;

public record ProviderStatusAuditInfo(ProviderStatus? InitialStatus, ProviderStatus UpdatedStatus);
