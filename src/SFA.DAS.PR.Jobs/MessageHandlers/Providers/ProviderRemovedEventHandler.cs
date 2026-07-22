using Microsoft.Extensions.Logging;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Constants;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Models;
using SFA.DAS.PR.Jobs.OuterApi.Requests;
using SFA.DAS.RoATPService.Application.Events;

namespace SFA.DAS.PR.Jobs.MessageHandlers.Providers;

public class ProviderRemovedEventHandler(
    ILogger<ProviderRemovedEventHandler> _logger,
    IProviderRelationshipsDataContext _providerRelationshipsDataContext,
    IPermissionRepository _permissionRepository,
    IProviderRepository _providerRepository,
    IProviderRelationshipsApiClient _providerRelationshipsApiClient
) : IHandleMessages<ProviderRemovedEvent>
{
    public async Task Handle(ProviderRemovedEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation(
            "{MessageHandlerName} was triggered by MessageId:{MessageId} for Ukprn:{Ukprn}",
            nameof(ProviderRemovedEventHandler),
            context.MessageId,
            message.Ukprn);

        var provider = await _providerRepository.GetProvider(message.Ukprn, context.CancellationToken);

        if (provider is null || provider.Status == ProviderStatus.Removed)
        {
            _logger.LogWarning("Provider not found or already removed for Ukprn:{Ukprn}",
                message.Ukprn);
            return;
        }

        provider.Status = ProviderStatus.Removed;
        provider.Updated = DateTime.UtcNow;

        _providerRelationshipsDataContext.JobAudits.Add(
            new JobAudit(
                nameof(ProviderRemovedEventHandler),
                new EventHandlerJobInfo<ProviderRemovedEvent>(
                    context.MessageId,
                    message,
                    true,
                    null)));

        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);

        var accountLegalEntityIds =
            await _permissionRepository.GetAccountLegalEntityIdsWithPermissionsByProviderUkprn(
                message.Ukprn,
                context.CancellationToken);

        if (accountLegalEntityIds.Count == 0)
        {
            return;
        }

        var removePermissionTasks = accountLegalEntityIds.Select(accountLegalEntityId =>
            _providerRelationshipsApiClient.RemovePermission(
                new RemovePermissionsRequest
                {
                    UserRef = SystemUserReference.ProviderRemovedEventHandler,
                    Ukprn = message.Ukprn,
                    AccountLegalEntityId = accountLegalEntityId
                },
                context.CancellationToken));

        await Task.WhenAll(removePermissionTasks);
    }
}
