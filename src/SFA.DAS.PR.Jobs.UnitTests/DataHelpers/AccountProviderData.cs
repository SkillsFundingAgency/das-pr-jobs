using AutoFixture;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

public static class AccountProviderData
{
    public static AccountProvider Create(long accountId)
    {
        Fixture fixture = TestHelpers.CreateFixture();

        return fixture
            .Build<AccountProvider>()
            .With(a => a.AccountId, accountId)
            .Without(a => a.AccountProviderLegalEntities)
            .Without(a => a.Account)
            .Create();
    }
}
