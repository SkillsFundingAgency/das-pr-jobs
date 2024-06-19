﻿using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data;
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
}
