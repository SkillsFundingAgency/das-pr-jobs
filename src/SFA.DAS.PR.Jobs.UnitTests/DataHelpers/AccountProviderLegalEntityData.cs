using AutoFixture;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

public static class AccountProviderLegalEntityData
{
    public static AccountProviderLegalEntity Create(long accountId, long accountLegalEntityId)
    {
        Fixture fixture = TestHelpers.CreateFixture();
        var accountLegalEntity = AccountLegalEntityData.Create(accountId, accountLegalEntityId);
        var accountProvider = AccountProviderData.Create(accountId);

        var accountProviderLegalEntity = fixture
            .Build<AccountProviderLegalEntity>()
            .With(a => a.AccountProvider, accountProvider)
            .With(a => a.AccountLegalEntity, accountLegalEntity)
            .Without(a => a.Permissions)
            .Create();

        var permissions = PermissionData.Create(accountProviderLegalEntity.Id);

        accountProviderLegalEntity.Permissions = [permissions];

        return accountProviderLegalEntity;
    }
}
