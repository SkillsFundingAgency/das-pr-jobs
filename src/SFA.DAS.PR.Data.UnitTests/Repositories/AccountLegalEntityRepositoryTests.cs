using NUnit.Framework;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.UnitTests;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Data.UnitTests.Repositories;

public class AccountLegalEntityRepositoryTests
{
    [Test]
    public async Task AccountLegalEntityRepository_GetAccountLegalEntity_Returns_Success()
    {
        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(1, 1);

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccountLegalEntity(accountLegalEntity)
            .PersistChanges();

        AccountLegalEntityRepository sut = new AccountLegalEntityRepository(context);
        var result = await sut.GetAccountLegalEntity(accountLegalEntity.Id, CancellationToken.None);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task AccountLegalEntityRepository_GetAccountLegalEntity_Returns_Null()
    {
        using var context = DbContextHelper.CreateInMemoryDbContext();
        AccountLegalEntityRepository sut = new AccountLegalEntityRepository(context);
        var result = await sut.GetAccountLegalEntity(0, CancellationToken.None);
        Assert.That(result, Is.Null);
    }
}
