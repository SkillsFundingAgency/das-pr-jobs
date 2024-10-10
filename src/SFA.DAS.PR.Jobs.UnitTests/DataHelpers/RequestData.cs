using AutoFixture;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

public sealed class RequestData
{
    public static Request Create(Guid requestId)
    {
        Fixture fixture = TestHelpers.CreateFixture();
        return fixture
            .Build<Request>()
            .With(a => a.Id, requestId)
            .Create();
    }
}
