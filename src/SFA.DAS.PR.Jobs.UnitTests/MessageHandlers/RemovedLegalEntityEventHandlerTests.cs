using System.Text.Json;
using AutoFixture.NUnit3;
using FluentAssertions.Execution;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;
using SFA.DAS.PR.Jobs.Models;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Jobs.UnitTests.MessageHandlers;

public class RemovedLegalEntityEventHandlerTests
{
    [Test]
    [AutoData]
    public async Task Handle_NoAccountLegalEntityExists_AddsAuditOnly(RemovedLegalEntityEvent message, string messageId)
    {
        using var dbContext = DbContextHelper.CreateInMemoryDbContext();

        RemovedLegalEntityEventHandler sut = new(dbContext, Mock.Of<ILogger<RemovedLegalEntityEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        var jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<RemovedLegalEntityEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            jobAudit.Should().NotBeNull();
            jobAudit.JobName.Should().Be(nameof(RemovedLegalEntityEventHandler));
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(RemovedLegalEntityEventHandler.RemovedLegalEntityEventFailureReason);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeFalse();
        }
    }

    [Test]
    [AutoData]
    public async Task Handle_AccountLegalEntityExists_RemovePermissions(RemovedLegalEntityEvent message, string messageId)
    {
        var accountProviderLegalEntity = AccountProviderLegalEntityData.Create(message.AccountId, message.AccountLegalEntityId);

        using var dbContext = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccountProviderLegalEntity(accountProviderLegalEntity)
            .PersistChanges();

        RemovedLegalEntityEventHandler sut = new(dbContext, Mock.Of<ILogger<RemovedLegalEntityEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        var jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<PermissionAuditDetails>>(jobAudit!.JobInfo!)!;

        var expectedInfo = new PermissionAuditDetails(
            message.AccountId, 
            message.AccountLegalEntityId, 
            accountProviderLegalEntity.AccountProvider.ProviderUkprn,
            [accountProviderLegalEntity.Permissions.First().Operation]
        );

        using (new AssertionScope())
        {
            dbContext.Permissions.Count().Should().Be(0);
            dbContext.AccountProviderLegalEntities.Count().Should().Be(0);
            jobAudit.Should().NotBeNull();
            jobAudit.JobName.Should().Be(nameof(RemovedLegalEntityEventHandler));
            info.MessageId.Should().Be(messageId);
            info.Event.Should().BeEquivalentTo(expectedInfo);
            info.IsSuccess.Should().BeTrue();
        }
    }
}
