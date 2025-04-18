﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;

public class AddedLegalEntityEventHandler(
    ILogger<AddedLegalEntityEventHandler> _logger,
    IProviderRelationshipsDataContext _providerRelationshipsDataContext,
    IEmployerAccountsApiClient _employerAccountsApiClient
) : IHandleMessages<AddedLegalEntityEvent>
{
    public const string AccountLegalEntityAlreadyExistsFailureReason = "Account legal entity already exists";

    public async Task Handle(AddedLegalEntityEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("{MessageHandlerName} was triggered by MessageId:{MessageId} for AccountId:{AccountId} and AccountLegalEntityId:{AccountLegalEntityId}", nameof(AddedLegalEntityEventHandler), context.MessageId, message.AccountId, message.AccountLegalEntityId);

        AccountLegalEntity? accountLegalEntity = await _providerRelationshipsDataContext
            .AccountLegalEntities
            .FirstOrDefaultAsync(a => a.AccountId == message.AccountId && a.Id == message.AccountLegalEntityId, context.CancellationToken);

        if (accountLegalEntity != null)
        {
            _logger.LogWarning("Legal entity with Id:{LegalEntityId} already exists", message.LegalEntityId);

            JobAudit jobAudit = new(
                nameof(AddedLegalEntityEventHandler),
                new EventHandlerJobInfo<AddedLegalEntityEvent>(context.MessageId, message, false, AccountLegalEntityAlreadyExistsFailureReason)
            );

            _providerRelationshipsDataContext.JobAudits.Add(jobAudit);
        }
        else
        {
            Account? providerRelationshipsAccount = await _providerRelationshipsDataContext.Accounts.FirstOrDefaultAsync(a => a.Id == message.AccountId, context.CancellationToken);

            if (providerRelationshipsAccount is null)
            {
                var accountResponse = await _employerAccountsApiClient.GetAccount(message.AccountId, context.CancellationToken);

                providerRelationshipsAccount = new Account()
                {
                    Id = message.AccountId,
                    HashedId = accountResponse.HashedAccountId,
                    PublicHashedId = accountResponse.PublicHashedAccountId,
                    Name = accountResponse.DasAccountName,
                    Created = DateTime.UtcNow
                };

                _providerRelationshipsDataContext.Accounts.Add(providerRelationshipsAccount);
            }

            var newAccountLegalEntity = new AccountLegalEntity()
            {
                Id = message.AccountLegalEntityId,
                Account = providerRelationshipsAccount!,
                PublicHashedId = message.AccountLegalEntityPublicHashedId,
                Name = message.OrganisationName,
                Created = message.Created
            };

            _providerRelationshipsDataContext.AccountLegalEntities.Add(newAccountLegalEntity);

            JobAudit jobAudit = new JobAudit(
                nameof(AddedLegalEntityEventHandler),
                new EventHandlerJobInfo<AddedLegalEntityEvent>(context.MessageId, message, true, null)
            );

            _providerRelationshipsDataContext.JobAudits.Add(jobAudit);

            _logger.LogInformation("Created new legal entity with Id: {LegalEntityId} associated with employer account with Id:{EmployerAccountId}", message.LegalEntityId, message.AccountId);
        }

        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);
    }
}
