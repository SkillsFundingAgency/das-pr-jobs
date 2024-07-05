using AutoFixture;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

public class PermissionData
{
    public static Permission Create(long accountProviderLegalEntityId)
    {
        Fixture fixture = TestHelpers.CreateFixture();
        return fixture
            .Build<Permission>()
            .Without(a => a.AccountProviderLegalEntity)
            .With(a => a.AccountProviderLegalEntityId, accountProviderLegalEntityId)
            .Create();
    }
}
