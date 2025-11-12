using System.Text.Json;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.PR.Jobs.Functions;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.Models;

namespace SFA.DAS.PR.Jobs.UnitTests.Functions;
public class UpdateProvidersFunctionTests
{
    [Test]
    public async Task Run_NoChangesToProvidersData_SaveAuditOnly()
    {
        CancellationToken cancellationToken = new();
        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddProviders()
            .PersistChanges();

        Mock<IRoatpServiceApiClient> apiClientMock = new();
        var registeredProviders = context.Providers.Select(p => new RegisteredProviderInfo(p.Ukprn, p.Name));
        apiClientMock.Setup(a => a.GetProviders(cancellationToken)).ReturnsAsync(new RegisteredProviderResponse(registeredProviders));

        UpdateProvidersFunction sut = new(Mock.Of<ILogger<UpdateProvidersFunction>>(), apiClientMock.Object, context);

        await sut.Run(new(), cancellationToken);

        using (new AssertionScope())
        {
            context.Providers.Count().Should().Be(5);
            context.JobAudits.Count().Should().Be(1);
            var info = JsonSerializer.Deserialize<ProviderUpdateJobInfo>(context.JobAudits.First().JobInfo!)!;
            info.TotalRegisteredProviders.Should().Be(5);
            info.TotalProvidersAdded.Should().Be(0);
            info.TotalProvidersUpdated.Should().Be(0);
        }
    }

    [Test]
    public async Task Run_RegisteredProvidersDataIsUpdated_PersistUpdatesAndAudit()
    {
        CancellationToken cancellationToken = new();
        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddProviders()
            .PersistChanges();

        Mock<IRoatpServiceApiClient> apiClientMock = new();
        var registeredProviders = context.Providers.Select(p => new RegisteredProviderInfo(p.Ukprn, Guid.NewGuid().ToString()));
        apiClientMock.Setup(a => a.GetProviders(cancellationToken)).ReturnsAsync(new RegisteredProviderResponse(registeredProviders));

        UpdateProvidersFunction sut = new(Mock.Of<ILogger<UpdateProvidersFunction>>(), apiClientMock.Object, context);

        await sut.Run(new(), cancellationToken);

        using (new AssertionScope())
        {
            context.Providers.Count().Should().Be(5);
            context.JobAudits.Count().Should().Be(1);
            var info = JsonSerializer.Deserialize<ProviderUpdateJobInfo>(context.JobAudits.First().JobInfo!)!;
            info.TotalRegisteredProviders.Should().Be(5);
            info.TotalProvidersAdded.Should().Be(0);
            info.TotalProvidersUpdated.Should().Be(5);
        }
    }

    [Test]
    public async Task Run_NewRegisteredProvider_PersistNewProviderAndAudit()
    {
        CancellationToken cancellationToken = new();
        using var context = DbContextHelper
            .CreateInMemoryDbContext()
            .AddProviders()
            .PersistChanges();

        Mock<IRoatpServiceApiClient> apiClientMock = new();
        RegisteredProviderInfo newRegisteredProvider = new(11001100, Guid.NewGuid().ToString());
        apiClientMock.Setup(a => a.GetProviders(cancellationToken)).ReturnsAsync(new RegisteredProviderResponse([newRegisteredProvider]));

        UpdateProvidersFunction sut = new(Mock.Of<ILogger<UpdateProvidersFunction>>(), apiClientMock.Object, context);

        await sut.Run(new(), cancellationToken);

        using (new AssertionScope())
        {
            context.Providers.Count().Should().Be(6);
            context.JobAudits.Count().Should().Be(1);
            var info = JsonSerializer.Deserialize<ProviderUpdateJobInfo>(context.JobAudits.First().JobInfo!)!;
            info.TotalRegisteredProviders.Should().Be(1);
            info.TotalProvidersAdded.Should().Be(1);
            info.TotalProvidersUpdated.Should().Be(0);
        }
    }
}
