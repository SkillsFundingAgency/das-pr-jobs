using System.Text.Json;
using AutoFixture.NUnit3;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;
using SFA.DAS.PR.Jobs.Models;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Jobs.UnitTests.MessageHandlers;

public class AddedLegalEntityEventHandlerTests
{
    [Test, AutoData]
    public async Task Handle_AccountLegalEntityExists_AddsAuditOnly(AddedLegalEntityEvent message, string messageId)
    {
        using var dbContext = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccountLegalEntity(AccountLegalEntityData.Create(message.AccountId, message.AccountLegalEntityId))
            .PersistChanges();

        AddedLegalEntityEventHandler sut = new(dbContext, Mock.Of<ILogger<AddedLegalEntityEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        var jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<AddedLegalEntityEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            dbContext.AccountLegalEntities.Count().Should().Be(1);
            jobAudit.Should().NotBeNull();
            jobAudit.JobName.Should().Be(nameof(AddedLegalEntityEventHandler));
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(AddedLegalEntityEventHandler.AccountLegalEntityAlreadyExistsFailureReason);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeFalse();
        }
    }

    [Test, AutoData]
    public async Task Handle_AccountLegalEntityDoesNotExists_CreatesAccountLegalEntity(AddedLegalEntityEvent message, string messageId)
    {
        using var dbContext = DbContextHelper.CreateInMemoryDbContext();

        AddedLegalEntityEventHandler sut = new(dbContext, Mock.Of<ILogger<AddedLegalEntityEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        var jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<AddedLegalEntityEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            dbContext.AccountLegalEntities.Count().Should().Be(1);
            jobAudit.Should().NotBeNull();
            jobAudit.JobName.Should().Be(nameof(AddedLegalEntityEventHandler));
            info.MessageId.Should().Be(messageId);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeTrue();
        }
    }
}
