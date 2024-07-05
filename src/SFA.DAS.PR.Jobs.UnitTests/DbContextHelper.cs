using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Jobs.UnitTests;

public static class DbContextHelper
{
    public static ProviderRelationshipsDataContext CreateInMemoryDbContext()
    {
        var _dbContextOptions = new DbContextOptionsBuilder<ProviderRelationshipsDataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ProviderRelationshipsDataContext(_dbContextOptions);
    }

    public static ProviderRelationshipsDataContext AddProviders(this ProviderRelationshipsDataContext context)
    {
        context.Providers.AddRange(ProvidersData.GetProviders());
        return context;
    }

    public static ProviderRelationshipsDataContext PersistChanges(this ProviderRelationshipsDataContext context)
    {
        context.SaveChanges();
        return context;
    }

    public static ProviderRelationshipsDataContext AddAccountLegalEntity(this ProviderRelationshipsDataContext context, AccountLegalEntity accountLegalEntity)
    {
        context.AccountLegalEntities.Add(accountLegalEntity);
        return context;
    }

    public static ProviderRelationshipsDataContext AddAccount(this ProviderRelationshipsDataContext context, Account account)
    {
        context.Accounts.Add(account);
        return context;
    }

    public static ProviderRelationshipsDataContext AddAccountProviderLegalEntity(this ProviderRelationshipsDataContext context, AccountProviderLegalEntity accountProviderLegalEntity)
    {
        context.AccountProviderLegalEntities.Add(accountProviderLegalEntity);
        return context;
    }
}
