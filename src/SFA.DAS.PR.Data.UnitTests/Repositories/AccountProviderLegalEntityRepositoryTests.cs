using NUnit.Framework;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;
using SFA.DAS.PR.Jobs.UnitTests;
using Microsoft.EntityFrameworkCore;

namespace SFA.DAS.PR.Data.UnitTests.Repositories;

public sealed class AccountProviderLegalEntityRepositoryTests
{
    [Test]
    public async Task AccountProviderLegalEntityRepository_AddAccountProviderLegalEntity_Persists()
    {
        Account account = AccountData.Create(1);
        Provider provider = ProvidersData.Create();
        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(account, 1);
        AccountProvider accountProvider = AccountProviderData.Create(account.Id);
        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddProvider(provider)
            .AddAccount(account)
            .AddAccountProvider(accountProvider)
            .AddAccountLegalEntity(accountLegalEntity)
            .PersistChanges();

        AccountProviderLegalEntityRepository sut = new AccountProviderLegalEntityRepository(context);
        await sut.AddAccountProviderLegalEntity(
            new AccountProviderLegalEntity() { 
                AccountProviderId = accountProvider.Id, 
                AccountLegalEntityId = accountLegalEntity.Id 
            }, 
            CancellationToken.None
        );
        await context.SaveChangesAsync(CancellationToken.None);

        var persisted = await context.AccountProviderLegalEntities.FirstAsync();

        Assert.Multiple(() =>
        {
            Assert.That(persisted, Is.Not.Null);
            Assert.That(persisted.AccountLegalEntityId, Is.EqualTo(accountLegalEntity.Id));
            Assert.That(persisted.AccountProviderId, Is.EqualTo(accountProvider.Id));
        });
    }

    [Test]
    public async Task AccountProviderLegalEntityRepository_GetAccountProviderLegalEntity_ReturnsResult()
    {
        Account account = AccountData.Create(1);
        Provider provider = ProvidersData.Create();
        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(account, 1);
        AccountProvider accountProvider = AccountProviderData.Create(account.Id);
        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddProvider(provider)
            .AddAccount(account)
            .AddAccountProvider(accountProvider)
            .AddAccountLegalEntity(accountLegalEntity)
            .AddAccountProviderLegalEntity(
                new AccountProviderLegalEntity() { 
                    AccountLegalEntityId = accountLegalEntity.Id, 
                    AccountProviderId = accountProvider.Id
                }
            )
            .PersistChanges();

        AccountProviderLegalEntityRepository sut = new AccountProviderLegalEntityRepository(context);
        var result = await sut.GetAccountProviderLegalEntity(accountProvider.Id, accountLegalEntity.Id, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task AccountProviderLegalEntityRepository_GetAccountProviderLegalEntity_ReturnsNull()
    {
        using var context = DbContextHelper.CreateInMemoryDbContext();

        AccountProviderLegalEntityRepository sut = new AccountProviderLegalEntityRepository(context);
        var result = await sut.GetAccountProviderLegalEntity(1, 1, CancellationToken.None);

        Assert.That(result, Is.Null);
    }
}
