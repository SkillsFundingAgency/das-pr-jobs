using NUnit.Framework;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.UnitTests;

namespace SFA.DAS.PR.Data.UnitTests.Repositories;

public sealed class RequestsRepositoryTests
{
    [Test]
    public async Task RequestsRepository_GetRequest_Returns_Success()
    {
        Guid requestId = Guid.NewGuid();
        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddRequest(requestId)
            .PersistChanges();

        RequestsRepository sut = new RequestsRepository(context);
        var result = await sut.GetRequest(requestId, CancellationToken.None);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task RequestsRepository_GetRequest_Returns_Null()
    {
        using var context = DbContextHelper.CreateInMemoryDbContext();
        RequestsRepository sut = new RequestsRepository(context);
        var result = await sut.GetRequest(Guid.NewGuid(), CancellationToken.None);
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task RequestsRepository_GetExpiredRequests_Returns_Collection()
    {
        var request = new Request()
        {
            Id = Guid.NewGuid(),
            RequestType = RequestType.CreateAccount,
            Ukprn = 12345678,
            AccountLegalEntityId = 1,
            RequestedDate = DateTime.UtcNow.AddDays(-40),
            Status = RequestStatus.New,
            RequestedBy = Guid.NewGuid().ToString()
        };

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddRequest(request)
            .PersistChanges();

        RequestsRepository sut = new RequestsRepository(context);
        var result = await sut.GetExpiredRequests(14, CancellationToken.None);
        Assert.That(result.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task RequestsRepository_GetExpiredRequests_Returns_Empty()
    {
        using var context = DbContextHelper.CreateInMemoryDbContext();
        RequestsRepository sut = new RequestsRepository(context);
        var result = await sut.GetExpiredRequests(14, CancellationToken.None);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task RequestsRepository_GetExpired_BoundaryLineRequests_Returns_Collection()
    {
        DateTime pastDate = DateTime.UtcNow.AddDays(-15);
        DateTime boundaryLine = new DateTime(pastDate.Year, pastDate.Month, pastDate.Day, 23, 59, 59);

        var request = new Request()
        {
            Id = Guid.NewGuid(),
            RequestType = RequestType.CreateAccount,
            Ukprn = 12345678,
            AccountLegalEntityId = 1,
            RequestedDate = boundaryLine,
            Status = RequestStatus.New,
            RequestedBy = Guid.NewGuid().ToString()
        };

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddRequest(request)
            .PersistChanges();

        RequestsRepository sut = new RequestsRepository(context);
        var result = await sut.GetExpiredRequests(14, CancellationToken.None);
        Assert.That(result.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task RequestsRepository_GetExpired_BoundaryLineRequests_Returns_Empty()
    {
        DateTime pastDate = DateTime.UtcNow.AddDays(-15);
        DateTime boundaryLine = new DateTime(pastDate.Year, pastDate.Month, pastDate.Day + 1, 00, 00, 01);

        var request = new Request()
        {
            Id = Guid.NewGuid(),
            RequestType = RequestType.CreateAccount,
            Ukprn = 12345678,
            AccountLegalEntityId = 1,
            RequestedDate = boundaryLine,
            Status = RequestStatus.New,
            RequestedBy = Guid.NewGuid().ToString()
        };

        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddRequest(request)
            .PersistChanges();

        RequestsRepository sut = new RequestsRepository(context);
        var result = await sut.GetExpiredRequests(14, CancellationToken.None);
        Assert.That(result.Count, Is.EqualTo(0));
    }
}
