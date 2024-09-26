using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using NUnit.Framework;
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
}
