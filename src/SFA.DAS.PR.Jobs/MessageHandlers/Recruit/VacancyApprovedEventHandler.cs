﻿using Esfa.Recruit.Vacancies.Client.Domain.Events;
using Microsoft.Extensions.Logging;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Models.Recruit;

namespace SFA.DAS.PR.Jobs.MessageHandlers.Recruit;

public sealed class VacancyApprovedEventHandler(
    ILogger<VacancyApprovedEventHandler> _logger, 
    IAccountProviderLegalEntityRepository _accountProviderLegalEntityRepository, 
    IRecruitApiClient _recruitApiClient, 
    IProviderRelationshipsDataContext _providerRelationshipsDataContext,
    IAccountLegalEntityRepository _accountLegalEntityRepository,
    IAccountProviderRepository _accountProviderRepository,
    IProviderRepository _providerRepository
) : IHandleMessages<VacancyApprovedEvent>
{
    public async Task Handle(VacancyApprovedEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Listening to {EventType}", nameof(VacancyApprovedEvent));

        LiveVacancyModel? liveVacancy = await _recruitApiClient.GetLiveVacancy(
            message.VacancyReference,
            context.CancellationToken
        );

        AccountLegalEntity? accountLegalEntity = await _accountLegalEntityRepository.GetAccountLegalEntity(
            liveVacancy.AccountPublicHashedId, 
            context.CancellationToken
        );

        if (accountLegalEntity is null)
        {
            _logger.LogInformation("AccountLegalEntity for {AccountPublicHashedId} does not exist.", liveVacancy.AccountPublicHashedId);
            return;
        }

        Provider? provider = await _providerRepository.GetProvider(
            liveVacancy.TrainingProvider!.Ukprn, 
            context.CancellationToken
        );

        if(provider is null)
        {
            _logger.LogInformation("Provider for {Ukprn} does not exist.", liveVacancy.TrainingProvider!.Ukprn);
            return;
        }

        AccountProvider? accountProvider = await _accountProviderRepository.GetAccountProvider(
            provider.Ukprn,
            accountLegalEntity.AccountId,
            context.CancellationToken
        );

        if (accountProvider is null)
        {
            _logger.LogInformation("AccountProvider for {Ukprn} and {AccountId} does not exist.", provider.Ukprn, accountLegalEntity.AccountId);

            accountProvider = new AccountProvider()
            {
                AccountId = accountLegalEntity.AccountId,
                ProviderUkprn = provider.Ukprn
            };

            await _accountProviderRepository.AddAccountProvider(
                accountProvider, 
                context.CancellationToken
            );

            await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);
        }

        AccountProviderLegalEntity? accountProviderLegalEntity = await _accountProviderLegalEntityRepository.GetAccountProviderLegalEntity(
            accountProvider!.Id,
            accountLegalEntity.Id,
            context.CancellationToken
        );

        if(accountProviderLegalEntity is not null)
        {
            _logger.LogInformation("AccountProviderLegalEntity is not null for {AccountProviderId} and {AccountLegalEntityId} does not exist.", accountProvider!.Id, accountLegalEntity.Id);
            return;
        }

        await _accountProviderLegalEntityRepository.AddAccountProviderLegalEntity(
            new AccountProviderLegalEntity()
            {
                AccountLegalEntityId = accountLegalEntity.Id,
                AccountProviderId = accountProvider!.Id,
                Created = DateTime.UtcNow
            },
            context.CancellationToken
        );

        PermissionAudit permissionAudit = CreatePermissionAudit(provider, accountLegalEntity);

        await _providerRelationshipsDataContext.PermissionAudits.AddAsync(permissionAudit, context.CancellationToken);

        Notification notification = CreateNotification("LinkedAccountRecruit", "PR Jobs: VacancyReviewedEvent", accountLegalEntity.Id, liveVacancy);

        await _providerRelationshipsDataContext.Notifications.AddAsync(notification, context.CancellationToken);

        await _providerRelationshipsDataContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("VacancyApprovedEvent completed.");
    }

    private static PermissionAudit CreatePermissionAudit(Provider provider, AccountLegalEntity accountLegalEntity)
    {
        return new PermissionAudit
        {
            Eventtime = DateTime.UtcNow,
            Action = nameof(PermissionAction.RecruitRelationship),
            Ukprn = provider.Ukprn,
            AccountLegalEntityId = accountLegalEntity.Id,
            Operations = "[]"
        };
    }

    private static Notification CreateNotification(string templateName, string createdBy, long accountLegalEntityId, LiveVacancyModel vacancy)
    {
        return new Notification
        {
            TemplateName = templateName,
            NotificationType = nameof(NotificationType.Provider),
            Ukprn = vacancy.TrainingProvider!.Ukprn,
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow,
            AccountLegalEntityId = accountLegalEntityId
        };
    }
}