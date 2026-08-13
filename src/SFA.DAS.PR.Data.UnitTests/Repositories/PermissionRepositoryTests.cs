using AutoFixture.NUnit4;
using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.UnitTests;

namespace SFA.DAS.PR.Data.UnitTests.Repositories;

public class PermissionRepositoryTests
{
    [Test, AutoData]
    public async Task WhenPermissionsExist_ThenReturnsDistinctAccountLegalEntityIds(int providerUkprn)
    {
        using var dbContext = DbContextHelper.CreateInMemoryDbContext();

        var provider = new Provider
        {
            Ukprn = providerUkprn,
            Name = "Test Provider"
        };

        var accountProvider = new AccountProvider
        {
            ProviderUkprn = providerUkprn
        };

        var legalEntity1 = new AccountProviderLegalEntity
        {
            AccountLegalEntityId = 1001,
            AccountProvider = accountProvider
        };

        var legalEntity2 = new AccountProviderLegalEntity
        {
            AccountLegalEntityId = 1002,
            AccountProvider = accountProvider
        };

        var duplicateLegalEntity = new AccountProviderLegalEntity
        {
            AccountLegalEntityId = 1001,
            AccountProvider = accountProvider
        };

        dbContext.Providers.Add(provider);
        dbContext.AccountProviders.Add(accountProvider);
        dbContext.AccountProviderLegalEntities.AddRange(legalEntity1, legalEntity2, duplicateLegalEntity);
        dbContext.Permissions.AddRange(
            new Permission { AccountProviderLegalEntity = legalEntity1 },
            new Permission { AccountProviderLegalEntity = legalEntity2 },
            new Permission { AccountProviderLegalEntity = duplicateLegalEntity });

        dbContext.PersistChanges();

        var sut = new PermissionRepository(dbContext);

        var result = await sut.GetAccountLegalEntityIdsWithPermissionsByProviderUkprn(providerUkprn, CancellationToken.None);

        result.Should().BeEquivalentTo(new List<long> { 1001, 1002 });
    }

    [Test]
    public async Task WhenNoPermissionsExist_ThenReturnsEmptyList()
    {
        using var dbContext = DbContextHelper.CreateInMemoryDbContext();

        var sut = new PermissionRepository(dbContext);

        var result = await sut.GetAccountLegalEntityIdsWithPermissionsByProviderUkprn(12345678, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
