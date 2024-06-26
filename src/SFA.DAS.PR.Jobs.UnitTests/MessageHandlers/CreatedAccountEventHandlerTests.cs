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
public class CreatedAccountEventHandlerTests
{
    [Test, AutoData]
    public async Task Handle_AccountExists_AddsAuditOnly(CreatedAccountEvent message, string messageId)
    {
        using var dbContext = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccount(AccountData.Create(message.AccountId))
            .PersistChanges();

        CreatedAccountEventHandler sut = new(dbContext, Mock.Of<ILogger<CreatedAccountEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        var jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<CreatedAccountEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            dbContext.Accounts.Count().Should().Be(1);
            jobAudit.Should().NotBeNull();
            jobAudit.JobName.Should().Be(nameof(CreatedAccountEventHandler));
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(CreatedAccountEventHandler.AccountAlreadyExistsFailureReason);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeFalse();
        }
    }

    [Test, AutoData]
    public async Task Handle_AccountDoesNotExists_CreatesAccount(CreatedAccountEvent message, string messageId)
    {
        using var dbContext = DbContextHelper.CreateInMemoryDbContext();

        CreatedAccountEventHandler sut = new(dbContext, Mock.Of<ILogger<CreatedAccountEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        var jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<CreatedAccountEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            dbContext.Accounts.Count().Should().Be(1);
            jobAudit.Should().NotBeNull();
            jobAudit.JobName.Should().Be(nameof(CreatedAccountEventHandler));
            info.MessageId.Should().Be(messageId);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeTrue();
        }
    }
}
