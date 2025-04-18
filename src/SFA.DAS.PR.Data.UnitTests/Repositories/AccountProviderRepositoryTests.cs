﻿using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.UnitTests;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Data.UnitTests.Repositories;

public sealed class AccountProviderRepositoryTests
{
    [Test]
    public async Task AccountProviderRepository_GetProvider_Returns_Success()
    {
        Account account = AccountData.Create(1);
        Provider provider = ProvidersData.Create();
        AccountProvider accountProvider = AccountProviderData.Create(account.Id);
        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccount(account)
            .AddProvider(provider)
            .AddAccountProvider(accountProvider)
            .PersistChanges();

        AccountProviderRepository sut = new AccountProviderRepository(context);
        var result = await sut.GetAccountProvider(accountProvider.ProviderUkprn, account.Id, CancellationToken.None);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task AccountProviderRepository_GetAccountProvider_Returns_Entity()
    {
        AccountProvider accountProvider = AccountProviderData.Create(1);

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccountProvider(accountProvider)
            .PersistChanges();

        AccountProviderRepository sut = new AccountProviderRepository(context);
        var result = await sut.GetAccountProvider(accountProvider.ProviderUkprn, accountProvider.AccountId, CancellationToken.None);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task ProviderRepository_GetProvider_Returns_Null()
    {
        using var context = DbContextHelper.CreateInMemoryDbContext();
        AccountProviderRepository sut = new AccountProviderRepository(context);
        var result = await sut.GetAccountProvider(1, 1, CancellationToken.None);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AccountProviderRepository_GetProvider_Persists()
    {
        Account account = AccountData.Create(1);
        Provider provider = ProvidersData.Create();
        AccountProvider accountProvider = AccountProviderData.Create(account.Id);
        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccount(account)
            .AddProvider(provider)
            .PersistChanges();

        AccountProviderRepository repo = new AccountProviderRepository(context);
        await repo.AddAccountProvider(accountProvider, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        var sut = await context.AccountProviders.FirstAsync(CancellationToken.None);

        Assert.That(sut, Is.Not.Null);
    }
}
