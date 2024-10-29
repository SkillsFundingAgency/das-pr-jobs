using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.UnitTests;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Data.UnitTests.Repositories;

public sealed class AccountProviderRepositoryTests
{
    [Test]
    public async Task AccountProviderRepository_GetAccountProvider_Returns_Entity()
    {
        AccountProvider accountProvider = AccountProviderData.Create(1);
       
        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccountProvider(accountProvider)
            .PersistChanges();

        AccountProviderRepository sut = new AccountProviderRepository(context);
        var result = await sut.GetAccountProvider(accountProvider.AccountId, accountProvider.ProviderUkprn, CancellationToken.None);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task NotificationsRepository_GetPendingNotifications_Returns_Null()
    {
        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .PersistChanges();

        AccountProviderRepository sut = new AccountProviderRepository(context);
        var result = await sut.GetAccountProvider(1, 1, CancellationToken.None);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AccountProviderRepository_CreateAccountProvider_Persists()
    {
        AccountProvider accountProvider = AccountProviderData.Create(1);

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .PersistChanges();

        AccountProviderRepository repository = new AccountProviderRepository(context);
        var result = await repository.CreateAccountProvider(accountProvider, CancellationToken.None);

        await context.SaveChangesAsync(CancellationToken.None);

        var sut = await  context.AccountProviders.FirstOrDefaultAsync(CancellationToken.None);

        Assert.That(sut, Is.Not.Null);
    }
}
