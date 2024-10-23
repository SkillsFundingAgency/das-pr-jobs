using NUnit.Framework;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.UnitTests;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Data.UnitTests.Repositories;

public sealed class ProviderRepositoryTests
{
    [Test]
    public async Task ProviderRepository_GetProvider_Returns_Success()
    {
        Provider provider = ProvidersData.Create();
        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddProvider(provider)
            .PersistChanges();

        ProviderRepository sut = new ProviderRepository(context);
        var result = await sut.GetProvider(provider.Ukprn, CancellationToken.None);
        Assert.That(result!.Ukprn, Is.EqualTo(provider.Ukprn));
    }

    [Test]
    public async Task ProviderRepository_GetProvider_Returns_Null()
    {
        using var context = DbContextHelper
            .CreateInMemoryDbContext();

        ProviderRepository sut = new ProviderRepository(context);
        var result = await sut.GetProvider(10000006, CancellationToken.None);
        Assert.That(result, Is.Null);
    }
}
