using AutoFixture;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

public sealed class RequestData
{
    public static Request Create(Guid requestId, RequestStatus? status = null)
    {
        Fixture fixture = TestHelpers.CreateFixture();
        return fixture
            .Build<Request>()
            .With(a => a.Id, requestId)
            .With(a => a.Status, status ?? RequestStatus.New)
            .Create();
    }
}
