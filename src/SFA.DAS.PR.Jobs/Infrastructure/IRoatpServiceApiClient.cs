﻿using Refit;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.Infrastructure;

public interface IRoatpServiceApiClient
{
    [Get("/api/v1/fat-data-export")]
    Task<IEnumerable<RegisteredProviderInfo>> GetProviders(CancellationToken cancellationToken);
}
