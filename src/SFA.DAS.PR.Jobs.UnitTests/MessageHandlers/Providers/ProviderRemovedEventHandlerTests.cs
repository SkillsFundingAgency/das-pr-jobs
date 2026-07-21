using System.Text.Json;
using AutoFixture.NUnit4;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Constants;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.MessageHandlers.Providers;
using SFA.DAS.PR.Jobs.Models;
using SFA.DAS.PR.Jobs.OuterApi.Requests;
using SFA.DAS.RoATPService.Application.Events;

namespace SFA.DAS.PR.Jobs.UnitTests.MessageHandlers.Providers;

public class ProviderRemovedEventHandlerTests
{
    [Test, AutoData]
    public async Task WhenProviderExists_ThenUpdatesProviderStatus(
     ProviderRemovedEvent message,
     string messageId)
    {
        using var dbContext = DbContextHelper.CreateInMemoryDbContext();

        dbContext.Providers.Add(new Provider
        {
            Name = "Test Provider",
            Ukprn = message.Ukprn,
            Status = null
        });

        dbContext.PersistChanges();

        Mock<IPermissionRepository> permissionRepository = new();
        permissionRepository
            .Setup(x => x.GetAccountLegalEntityIdsWithPermissionsByProviderUkprn(message.Ukprn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());

        Mock<IProviderRepository> providerRepository = new();
        providerRepository
            .Setup(x => x.GetProvider(message.Ukprn, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Provider?>(dbContext.Providers.Single()));

        Mock<IProviderRelationshipsApiClient> providerRelationshipsApiClient = new();

        ProviderRemovedEventHandler sut = new(
            Mock.Of<ILogger<ProviderRemovedEventHandler>>(),
            dbContext,
            permissionRepository.Object,
            providerRepository.Object,
            providerRelationshipsApiClient.Object);

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);
        messageContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await sut.Handle(message, messageContext.Object);

        var provider = dbContext.Providers.Single();

        using (new AssertionScope())
        {
            provider.Status.Should().Be(ProviderStatus.Removed);
            provider.Updated.Should().NotBeNull();
        }
    }

    [Test, AutoData]
    public async Task WhenPermissionsExist_ThenRemovesPermissions(
    ProviderRemovedEvent message,
    string messageId)
    {
        using var dbContext = DbContextHelper.CreateInMemoryDbContext();

        dbContext.Providers.Add(new Provider
        {
            Name = "Test Provider",
            Ukprn = message.Ukprn,
            Status = null
        });

        dbContext.PersistChanges();

        var accountLegalEntityIds = new List<long> { 1001, 1002, 1003 };

        Mock<IPermissionRepository> permissionRepository = new();
        permissionRepository
            .Setup(x => x.GetAccountLegalEntityIdsWithPermissionsByProviderUkprn(message.Ukprn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountLegalEntityIds);

        Mock<IProviderRepository> providerRepository = new();
        providerRepository
            .Setup(x => x.GetProvider(message.Ukprn, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Provider?>(dbContext.Providers.Single()));

        Mock<IProviderRelationshipsApiClient> providerRelationshipsApiClient = new();
        providerRelationshipsApiClient
            .Setup(x => x.RemovePermission(It.IsAny<RemovePermissionsRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ProviderRemovedEventHandler sut = new(
            Mock.Of<ILogger<ProviderRemovedEventHandler>>(),
            dbContext,
            permissionRepository.Object,
            providerRepository.Object,
            providerRelationshipsApiClient.Object);

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);
        messageContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await sut.Handle(message, messageContext.Object);

        providerRelationshipsApiClient.Verify(
            x => x.RemovePermission(
                It.Is<RemovePermissionsRequest>(r =>
                    r.Ukprn == message.Ukprn &&
                    accountLegalEntityIds.Contains(r.AccountLegalEntityId) &&
                    r.UserRef == SystemUserReference.ProviderRemovedEventHandler),
                It.IsAny<CancellationToken>()),
            Times.Exactly(accountLegalEntityIds.Count));
    }

    [Test, AutoData]
    public async Task WhenProviderExists_ThenAddsAudit(
    ProviderRemovedEvent message,
    string messageId)
    {
        using var dbContext = DbContextHelper.CreateInMemoryDbContext();

        dbContext.Providers.Add(new Provider
        {
            Name = "Test Provider",
            Ukprn = message.Ukprn,
            Status = null
        });

        dbContext.PersistChanges();

        Mock<IPermissionRepository> permissionRepository = new();
        permissionRepository
            .Setup(x => x.GetAccountLegalEntityIdsWithPermissionsByProviderUkprn(message.Ukprn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());

        Mock<IProviderRepository> providerRepository = new();
        providerRepository
            .Setup(x => x.GetProvider(message.Ukprn, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Provider?>(dbContext.Providers.Single()));

        Mock<IProviderRelationshipsApiClient> providerRelationshipsApiClient = new();

        ProviderRemovedEventHandler sut = new(
            Mock.Of<ILogger<ProviderRemovedEventHandler>>(),
            dbContext,
            permissionRepository.Object,
            providerRepository.Object,
            providerRelationshipsApiClient.Object);

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);
        messageContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await sut.Handle(message, messageContext.Object);

        var jobAudit = dbContext.JobAudits.Single(x => x.JobName == nameof(ProviderRemovedEventHandler));
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<ProviderRemovedEvent>>(jobAudit.JobInfo!)!;

        using (new AssertionScope())
        {
            jobAudit.JobName.Should().Be(nameof(ProviderRemovedEventHandler));
            info.MessageId.Should().Be(messageId);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeTrue();
            info.FailureReason.Should().BeNull();
        }
    }

    [Test, AutoData]
    public async Task WhenProviderDoesNotExist_ThenReturns(
    ProviderRemovedEvent message,
    string messageId)
    {
        using var dbContext = DbContextHelper.CreateInMemoryDbContext();

        Mock<IPermissionRepository> permissionRepository = new();
        Mock<IProviderRepository> providerRepository = new();
        providerRepository
            .Setup(x => x.GetProvider(message.Ukprn, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Provider?>(result: null));

        Mock<IProviderRelationshipsApiClient> providerRelationshipsApiClient = new();

        ProviderRemovedEventHandler sut = new(
            Mock.Of<ILogger<ProviderRemovedEventHandler>>(),
            dbContext,
            permissionRepository.Object,
            providerRepository.Object,
            providerRelationshipsApiClient.Object);

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);
        messageContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await sut.Handle(message, messageContext.Object);

        using (new AssertionScope())
        {
            dbContext.Providers.Should().BeEmpty();
            dbContext.JobAudits.Should().BeEmpty();
        }

        permissionRepository.Verify(
            x => x.GetAccountLegalEntityIdsWithPermissionsByProviderUkprn(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        providerRelationshipsApiClient.Verify(
            x => x.RemovePermission(
                It.IsAny<RemovePermissionsRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
