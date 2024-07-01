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

public class UpdatedLegalEntityEventHandlerTests
{
    [Test]
    [MoqAutoData]
    public async Task Handle_UpdatedLegalEntityEvent_UpdatesLegalEntityDetails(string messageId, long accountId, UpdatedLegalEntityEvent message)
    {
        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(accountId, message.AccountLegalEntityId);

        accountLegalEntity.Deleted = null;
        accountLegalEntity.Updated = DateTime.UtcNow;

        message.Created = DateTime.UtcNow.AddDays(1);

        using var dbContext = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccountLegalEntity(accountLegalEntity)
            .PersistChanges();

        UpdatedLegalEntityEventHandler sut = new(dbContext, Mock.Of<ILogger<UpdatedLegalEntityEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        AccountLegalEntity? updatedAccountLegalEntity = await dbContext.AccountLegalEntities.FirstOrDefaultAsync(a => a.Id == accountLegalEntity.Id, messageContext.Object.CancellationToken);

        JobAudit? jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<UpdatedLegalEntityEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            jobAudit.Should().NotBeNull();
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(null);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeTrue();

            updatedAccountLegalEntity!.Name.Should().Be(message.Name);
            updatedAccountLegalEntity!.Updated.Should().Be(message.Created);
        }
    }

    [Test]
    [MoqAutoData]
    public async Task Handle_UpdatedLegalEntityEvent_AccountLegalEntity_Null_AddsJobAuditOnly(string messageId, UpdatedLegalEntityEvent message)
    {
        using var dbContext = DbContextHelper.CreateInMemoryDbContext();

        UpdatedLegalEntityEventHandler sut = new(dbContext, Mock.Of<ILogger<UpdatedLegalEntityEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        JobAudit? jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<UpdatedLegalEntityEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            jobAudit.Should().NotBeNull();
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(UpdatedLegalEntityEventHandler.AccountLegalEntityNullFailureReason);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeFalse();
        }
    }

    [Test]
    [MoqAutoData]
    public async Task Handle_UpdatedLegalEntityEvent_AccountLegalEntity_Deleted_AddsJobAuditOnly(long accountId, string messageId, UpdatedLegalEntityEvent message)
    {
        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(accountId, message.AccountLegalEntityId);

        accountLegalEntity.Deleted = DateTime.UtcNow;

        using var dbContext = DbContextHelper.CreateInMemoryDbContext()
            .AddAccountLegalEntity(accountLegalEntity)
            .PersistChanges();

        UpdatedLegalEntityEventHandler sut = new(dbContext, Mock.Of<ILogger<UpdatedLegalEntityEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        JobAudit? jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<UpdatedLegalEntityEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            jobAudit.Should().NotBeNull();
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(UpdatedLegalEntityEventHandler.AccountLegalEntityDeleteFailureReason);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeFalse();
        }
    }

    [Test]
    [MoqAutoData]
    public async Task Handle_UpdatedLegalEntityEvent_AccountLegalEntity_NameMatch_AddsJobAuditOnly(long accountId, string messageId, UpdatedLegalEntityEvent message)
    {
        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(accountId, message.AccountLegalEntityId);

        accountLegalEntity.Deleted = null;
        accountLegalEntity.Name = message.Name;

        using var dbContext = DbContextHelper.CreateInMemoryDbContext()
            .AddAccountLegalEntity(accountLegalEntity)
            .PersistChanges();

        UpdatedLegalEntityEventHandler sut = new(dbContext, Mock.Of<ILogger<UpdatedLegalEntityEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        JobAudit? jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<UpdatedLegalEntityEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            jobAudit.Should().NotBeNull();
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(UpdatedLegalEntityEventHandler.AccountLegalEntityNameMatchFailureReason);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeFalse();
        }
    }

    [Test]
    [MoqAutoData]
    public async Task Handle_UpdatedLegalEntityEvent_AccountLegalEntity_InvalidCreatedDate_AddsJobAuditOnly(long accountId, string messageId, UpdatedLegalEntityEvent message)
    {
        AccountLegalEntity accountLegalEntity = AccountLegalEntityData.Create(accountId, message.AccountLegalEntityId);

        accountLegalEntity.Deleted = null;
        accountLegalEntity.Updated = DateTime.UtcNow;
        message.Created = DateTime.UtcNow.AddDays(-1);

        using var dbContext = DbContextHelper.CreateInMemoryDbContext()
            .AddAccountLegalEntity(accountLegalEntity)
            .PersistChanges();

        UpdatedLegalEntityEventHandler sut = new(dbContext, Mock.Of<ILogger<UpdatedLegalEntityEventHandler>>());

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        JobAudit? jobAudit = dbContext.JobAudits.FirstOrDefault();
        jobAudit.Should().NotBeNull();
        var info = JsonSerializer.Deserialize<EventHandlerJobInfo<UpdatedLegalEntityEvent>>(jobAudit!.JobInfo!)!;
        using (new AssertionScope())
        {
            jobAudit.Should().NotBeNull();
            info.MessageId.Should().Be(messageId);
            info.FailureReason.Should().Be(UpdatedLegalEntityEventHandler.AccountLegalEntityDateFailureReason);
            info.Event.Should().BeEquivalentTo(message);
            info.IsSuccess.Should().BeFalse();
        }
    }
}