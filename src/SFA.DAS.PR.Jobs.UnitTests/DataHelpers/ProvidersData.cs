using AutoFixture;
using Humanizer;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.PR.Jobs.UnitTests.DataHelpers;
public static class ProvidersData
{
    public readonly static long[] ValidUkprns = { 10011001, 10011002, 10011003, 10011004, 10011005 };

    public static Provider Create()
    {
        Fixture fixture = TestHelpers.CreateFixture();
        var provider = fixture
            .Build<Provider>()
            .With(a => a.Ukprn, ValidUkprns.First())
            .Without(a => a.AccountProviders)
            .Create();

        provider.Name = provider.Ukprn.ToWords();

        return provider;
    }

    public static List<Provider> GetProviders()
    {
        Fixture fixture = TestHelpers.CreateFixture();
        var providers = fixture
            .Build<Provider>()
            .CreateMany(p => p.Ukprn, ValidUkprns)
            .ToList();
        providers.ForEach(p => p.Name = p.Ukprn.ToWords());
        return providers;
    }
}
