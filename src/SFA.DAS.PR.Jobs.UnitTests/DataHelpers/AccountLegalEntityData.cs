using AutoFixture;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Jobs.UnitTests.DataHelpers;
public static class AccountLegalEntityData
{
    public static AccountLegalEntity Create(long accountId, long accountLegalEntityId)
    {
        Fixture fixture = TestHelpers.CreateFixture();
        var account = fixture
            .Build<Account>()
            .With(a => a.Id, accountId)
            .Without(a => a.AccountLegalEntities)
            .Without(a => a.AccountProviders)
            .Create();
        return fixture
            .Build<AccountLegalEntity>()
            .With(a => a.AccountId, accountId)
            .With(a => a.Id, accountLegalEntityId)
            .With(a => a.Account, account)
            .Without(a => a.AccountProviderLegalEntities)
            .Create();
    }
}
