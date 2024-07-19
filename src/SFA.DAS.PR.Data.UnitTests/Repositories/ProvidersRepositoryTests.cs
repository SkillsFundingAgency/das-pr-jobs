using NUnit.Framework;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.UnitTests;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Data.UnitTests.Repositories;

public class ProvidersRepositoryTests
{
    [Test]
    public async Task ProviderRepository_GetProvider_Returns_Success()
    {
        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddProviders()
            .PersistChanges();

        ProvidersRepository sut = new ProvidersRepository(context);
        var result = await sut.GetProvider(10011001, CancellationToken.None);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task ProviderRepository_GetProvider_Returns_Null()
    {
        using var context = DbContextHelper.CreateInMemoryDbContext();
        ProvidersRepository sut = new ProvidersRepository(context);
        var result = await sut.GetProvider(0, CancellationToken.None);
        Assert.That(result, Is.Null);
    }
}
