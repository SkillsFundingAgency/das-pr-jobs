using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;
using SFA.DAS.PR.Jobs.Models;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;
using SFA.DAS.Testing.AutoFixture;
using System.Text.Json;

namespace SFA.DAS.PR.Jobs.UnitTests.MessageHandlers;

public class ChangedAccountNameEventHandlerTests
{
    [Test]
    [MoqAutoData]
    public async Task Handle_AccountNameChanged_UpdatesAccountDetails(string messageId, ChangedAccountNameEvent message)
    {
        Account account = AccountData.Create(message.AccountId);
        account.Updated = DateTime.UtcNow;

        message.Created = DateTime.UtcNow.AddDays(1);

        using var dbContext = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccount(account)
            .PersistChanges();

        ChangedAccountNameEventHandler sut = new(dbContext, Mock.Of<ILogger<ChangedAccountNameEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        Account? updatedAccount = await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id, messageContext.Object.CancellationToken);

        JobAudit? jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<ChangedAccountNameEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            jobAudit.Should().NotBeNull();
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(null);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeTrue();

            updatedAccount!.Name.Should().Be(message.CurrentName);
            updatedAccount!.Updated.Should().Be(message.Created);
        }
    }

    [Test]
    [MoqAutoData]
    public async Task Handle_AccountNameChanged_NullUpdatedDate_UpdatesAccountDetails(string messageId, ChangedAccountNameEvent message)
    {
        message.Created = DateTime.UtcNow.AddDays(1);

        Account account = AccountData.Create(message.AccountId);
        account.Updated = null;

        using var dbContext = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccount(account)
            .PersistChanges();

        ChangedAccountNameEventHandler sut = new(dbContext, Mock.Of<ILogger<ChangedAccountNameEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        JobAudit? jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();

        Account? updatedAccount = await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id, messageContext.Object.CancellationToken);

        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<ChangedAccountNameEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            jobAudit.Should().NotBeNull();
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(null);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeTrue();

            updatedAccount!.Name.Should().Be(message.CurrentName);
            updatedAccount!.Updated.Should().Be(message.Created);
        }
    }

    [Test]
    [MoqAutoData]
    public async Task Handle_AccountNameChanged_InvalidUpdatedDate_AddsJobAuditOnly(string messageId, ChangedAccountNameEvent message)
    {
        message.Created = DateTime.UtcNow.AddDays(-3);

        Account account = AccountData.Create(message.AccountId);
        account.Updated = DateTime.UtcNow;

        using var dbContext = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccount(account)
            .PersistChanges();

        ChangedAccountNameEventHandler sut = new(dbContext, Mock.Of<ILogger<ChangedAccountNameEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        JobAudit? jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<ChangedAccountNameEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            jobAudit.Should().NotBeNull();
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(ChangedAccountNameEventHandler.AccountDateFailureReason);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeFalse();
        }
    }

    [Test]
    [MoqAutoData]
    public async Task Handle_AccountNameChanged_NullAccount_AddsJobAuditOnly(string messageId, ChangedAccountNameEvent message)
    {
        using var dbContext = DbContextHelper.CreateInMemoryDbContext();

        ChangedAccountNameEventHandler sut = new(dbContext, Mock.Of<ILogger<ChangedAccountNameEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        JobAudit? jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<ChangedAccountNameEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            jobAudit.Should().NotBeNull();
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(ChangedAccountNameEventHandler.AccountNullFailureReason);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeFalse();
        }
    }

    [Test]
    [MoqAutoData]
    public async Task Handle_AccountNameChanged_NameMatch_AddsJobAuditOnly(string messageId, ChangedAccountNameEvent message)
    {
        message.Created = DateTime.UtcNow.AddDays(1);

        Account account = AccountData.Create(message.AccountId);
        account.Updated = null;
        account.Name = message.CurrentName;

        using var dbContext = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccount(account)
            .PersistChanges();

        ChangedAccountNameEventHandler sut = new(dbContext, Mock.Of<ILogger<ChangedAccountNameEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        JobAudit? jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();

        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<ChangedAccountNameEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            jobAudit.Should().NotBeNull();
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(ChangedAccountNameEventHandler.AcountNameMatchFailureReason);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeFalse();
        }
    }
}