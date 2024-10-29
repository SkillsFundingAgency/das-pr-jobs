using NUnit.Framework;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;
using SFA.DAS.PR.Jobs.UnitTests;

namespace SFA.DAS.PR.Data.UnitTests.Repositories;

public sealed class AccountRepositoryTests
{
    [Test]
    public async Task AccountRepository_GetAccount_Returns_Entity()
    {
        Account account = AccountData.Create(1);

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccount(account)
            .PersistChanges();

        AccountRepository sut = new AccountRepository(context);
        var result = await sut.GetAccount(1, CancellationToken.None);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task AccountRepository_GetAccount_Returns_Null()
    {
        Account account = AccountData.Create(1);

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .PersistChanges();

        AccountRepository sut = new AccountRepository(context);
        var result = await sut.GetAccount(1, CancellationToken.None);
        Assert.That(result, Is.Null);
    }
}
