using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
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

            if(providerRelationshipsAccount is null)
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

                await _providerRelationshipsDataContext.Accounts.AddAsync(providerRelationshipsAccount, context.CancellationToken);
            }

            var newAccountLegalEntity = new AccountLegalEntity() {
                Id = message.AccountLegalEntityId,
                Account = providerRelationshipsAccount!,
                PublicHashedId = message.AccountLegalEntityPublicHashedId,
                Name = message.OrganisationName,
                Created = message.Created
            };

            await _providerRelationshipsDataContext.AccountLegalEntities.AddAsync(newAccountLegalEntity, context.CancellationToken);

            JobAudit jobAudit = new JobAudit(
                nameof(AddedLegalEntityEventHandler),
                new EventHandlerJobInfo<AddedLegalEntityEvent>(context.MessageId, message, true, null)
            );

            await _providerRelationshipsDataContext.JobAudits.AddAsync(jobAudit, context.CancellationToken);

            _logger.LogInformation("Created new legal entity with Id: {LegalEntityId} associated with employer account with Id:{EmployerAccountId}", message.LegalEntityId, message.AccountId);
        }

        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);
    }
}
