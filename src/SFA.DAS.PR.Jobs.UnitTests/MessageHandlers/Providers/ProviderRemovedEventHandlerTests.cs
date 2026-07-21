using System.Text.Json;
using AutoFixture.NUnit4;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.PR.Data.Common;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.MessageHandlers.Providers;
using SFA.DAS.PR.Jobs.Models;
using SFA.DAS.PR.Jobs.OuterApi.Requests;
using SFA.DAS.RoATPService.Application.Events;

namespace SFA.DAS.PR.Jobs.UnitTests.MessageHandlers.Providers;

public class ProviderRemovedEventHandlerTests
{
    [Test, AutoData]
    public async Task WhenProviderExistsAndPermissionsExist_ThenRemovesPermissions_AndUpdatesProviderStatusToRemoved_AndAddsAudits(
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

        dbContext.JobAudits.Should().HaveCount(2);

        var audits = dbContext.JobAudits.ToList();

        var handlerAudit = audits.Single(x => x.JobName == nameof(ProviderRemovedEventHandler));
        var statusAudit = audits.Single(x => x.JobName == "UpdateProvider");

        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<ProviderRemovedEvent>>(handlerAudit.JobInfo!)!;
        var statusInfo = JsonSerializer.Deserialize<FieldUpdateAuditInfo>(statusAudit.JobInfo!)!;

        using (new AssertionScope())
        {
            var provider = dbContext.Providers.Single();
            provider.Status.Should().Be(ProviderStatus.Removed);
            provider.Updated.Should().NotBeNull();

            handlerAudit.JobName.Should().Be(nameof(ProviderRemovedEventHandler));
            info.MessageId.Should().Be(messageId);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeTrue();
            info.FailureReason.Should().BeNull();

            statusAudit.JobName.Should().Be("UpdateProvider");
            statusInfo.FieldUpdated.Should().Be(nameof(Provider.Status));
            statusInfo.InitialState.Should().BeEmpty();
            statusInfo.UpdatedState.Should().Be(ProviderStatus.Removed.ToString());
        }

        providerRelationshipsApiClient.Verify(x => x.RemovePermission(
                It.Is<RemovePermissionsRequest>(r =>
                    r.Ukprn == message.Ukprn &&
                    accountLegalEntityIds.Contains(r.AccountLegalEntityId) &&
                    r.UserRef == SystemUserReferences.ProviderRemovedEventHandler),
                It.IsAny<CancellationToken>()),
            Times.Exactly(accountLegalEntityIds.Count));
    }

    [Test, AutoData]
    public async Task WhenNoPermissionsExist_ThenUpdatesProviderStatusToRemoved_AndAddsAudits_AndVerifyApiIsNotCalled(
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

        dbContext.JobAudits.Should().HaveCount(2);

        var audits = dbContext.JobAudits.ToList();

        var handlerAudit = audits.Single(x => x.JobName == nameof(ProviderRemovedEventHandler));
        var statusAudit = audits.Single(x => x.JobName == "UpdateProvider");

        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<ProviderRemovedEvent>>(handlerAudit.JobInfo!)!;
        var statusInfo = JsonSerializer.Deserialize<FieldUpdateAuditInfo>(statusAudit.JobInfo!)!;

        using (new AssertionScope())
        {
            var provider = dbContext.Providers.Single();
            provider.Status.Should().Be(ProviderStatus.Removed);
            provider.Updated.Should().NotBeNull();

            handlerAudit.JobName.Should().Be(nameof(ProviderRemovedEventHandler));
            info.MessageId.Should().Be(messageId);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeTrue();
            info.FailureReason.Should().BeNull();

            statusAudit.JobName.Should().Be("UpdateProvider");
            statusInfo.FieldUpdated.Should().Be(nameof(Provider.Status));
            statusInfo.InitialState.Should().BeEmpty();
            statusInfo.UpdatedState.Should().Be(ProviderStatus.Removed.ToString());
        }

        providerRelationshipsApiClient.Verify(x => x.RemovePermission(
                It.IsAny<RemovePermissionsRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test, AutoData]
    public async Task WhenNoPermissionsAndProviderNotFound_ThenProviderStatusIsNotUpdated_AndVerifyApiIsNotCalled_AndAddsAudit(
        ProviderRemovedEvent message,
        string messageId)
    {
        using var dbContext = DbContextHelper
            .CreateInMemoryDbContext()
            .PersistChanges();

        Mock<IPermissionRepository> permissionRepository = new();
        permissionRepository
            .Setup(x => x.GetAccountLegalEntityIdsWithPermissionsByProviderUkprn(message.Ukprn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long>());

        Mock<IProviderRepository> providerRepository = new();
        providerRepository
            .Setup(x => x.GetProvider(message.Ukprn, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Provider?>((Provider?)null));

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

        dbContext.JobAudits.Should().HaveCount(1);

        var jobAudit = dbContext.JobAudits.Single();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<ProviderRemovedEvent>>(jobAudit.JobInfo!)!;

        using (new AssertionScope())
        {
            jobAudit.JobName.Should().Be(nameof(ProviderRemovedEventHandler));
            info.MessageId.Should().Be(messageId);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeTrue();
            info.FailureReason.Should().BeNull();
            dbContext.Providers.Count().Should().Be(0);
        }

        providerRelationshipsApiClient.Verify(x => x.RemovePermission(
                It.IsAny<RemovePermissionsRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
