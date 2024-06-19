using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.Functions;

public class UpdateProvidersFunction(ILogger<UpdateProvidersFunction> _logger, IRoatpServiceApiClient _roatpClient, IProviderRelationshipsDataContext _providerRelationshipsDataContext)
{
    [Function(nameof(UpdateProvidersFunction))]
    public async Task Run([TimerTrigger("%UpdateProvidersFunctionSchedule%", RunOnStartup = false)] TimerInfo myTimer, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateProvidersFunction started at: {TimeStamp}", DateTime.UtcNow);

        var roatpProvidersTask = _roatpClient.GetProviders(cancellationToken);

        var existingProvidersTask = _providerRelationshipsDataContext.Providers.ToListAsync(cancellationToken);

        await Task.WhenAll(roatpProvidersTask, existingProvidersTask);

        var updatedProvidersCount = UpdateExistingProviders(roatpProvidersTask.Result, existingProvidersTask.Result);

        var newProvidersCount = AddNewProviders(roatpProvidersTask.Result, existingProvidersTask.Result);

        var summary = JsonSerializer.Serialize(new ProviderUpdateJobInfo(roatpProvidersTask.Result.Count(), newProvidersCount, updatedProvidersCount));

        _providerRelationshipsDataContext.JobAudits.Add(new() { JobName = nameof(UpdateProvidersFunction), JobInfo = summary });

        await _providerRelationshipsDataContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UpdateProvidersFunction completed at: {TimeStamp}", DateTime.UtcNow);
    }

    private static int UpdateExistingProviders(IEnumerable<RegisteredProviderInfo> roatpProviders, List<Provider> existingProviders)
    {
        var updateList =
            (from rProvider in roatpProviders
             join eProvider in existingProviders on rProvider.Ukprn equals eProvider.Ukprn
             where rProvider.LegalName != eProvider.Name
             select rProvider)
            .ToList();
        foreach (var rProvider in updateList)
        {
            var eProvider = existingProviders.First(e => e.Ukprn == rProvider.Ukprn);
            eProvider.Name = rProvider.LegalName;
            eProvider.Updated = DateTime.UtcNow;
        }
        return updateList.Count;
    }

    private int AddNewProviders(IEnumerable<RegisteredProviderInfo> roatpProviders, List<Provider> existingProviders)
    {
        var newProviders = roatpProviders.ExceptBy(existingProviders.Select(p => p.Ukprn), r => r.Ukprn).Select(r => CreateNewProvider(r));
        _providerRelationshipsDataContext.Providers.AddRange(newProviders);
        return newProviders.Count();
    }

    private static Provider CreateNewProvider(RegisteredProviderInfo info) =>
        new Provider { Name = info.LegalName, Ukprn = info.Ukprn, Created = DateTime.UtcNow };
}
