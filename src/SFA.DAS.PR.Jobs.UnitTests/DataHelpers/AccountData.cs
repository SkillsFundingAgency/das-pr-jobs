using AutoFixture;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Jobs.UnitTests.DataHelpers;
public static class AccountData
{
    public static Account Create(long accountId)
        => TestHelpers.CreateFixture()
            .Build<Account>()
            .With(a => a.Id, accountId)
            .Without(a => a.AccountLegalEntities)
            .Without(a => a.AccountProviders)
            .Create();
}
