using System.Text.Json;
using AutoFixture;
using AutoFixture.NUnit3;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.MessageHandlers.EmployerAccounts;
using SFA.DAS.PR.Jobs.Models;
using SFA.DAS.PR.Jobs.OuterApi.Responses;
using SFA.DAS.PR.Jobs.UnitTests.DataHelpers;

namespace SFA.DAS.PR.Jobs.UnitTests.MessageHandlers;

public class AddedLegalEntityEventHandlerTests
{
    [Test, AutoData]
    public async Task AddedLegalEntityEventHandlerTests_Handle_AccountLegalEntityExists_AddsAuditOnly(AddedLegalEntityEvent message, string messageId)
    {
        using var dbContext = DbContextHelper
            .CreateInMemoryDbContext()
            .AddAccountLegalEntity(AccountLegalEntityData.Create(message.AccountId, message.AccountLegalEntityId))
            .PersistChanges();

        IEmployerAccountsApiClient employerAccountsClient = Mock.Of<IEmployerAccountsApiClient>();

        AddedLegalEntityEventHandler sut = new(Mock.Of<ILogger<AddedLegalEntityEventHandler>>(), dbContext, employerAccountsClient);

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
    public async Task AddedLegalEntityEventHandlerTests_Handle_ProviderRelationshipsAccountIsNull_AddAccount(AddedLegalEntityEvent message, AccountDetails accountDetails, string messageId)
    {
        using var dbContext = DbContextHelper
            .CreateInMemoryDbContext()
            .PersistChanges();

        Mock<IEmployerAccountsApiClient> employerAccountsClient = new Mock<IEmployerAccountsApiClient>();
        employerAccountsClient.Setup(a =>
            a.GetAccount(message.AccountId, It.IsAny<CancellationToken>())
        ).ReturnsAsync(accountDetails);

        AddedLegalEntityEventHandler sut = new(Mock.Of<ILogger<AddedLegalEntityEventHandler>>(), dbContext, employerAccountsClient.Object);

        Mock<IMessageHandlerContext> messageContext = new();
        messageContext.Setup(c => c.MessageId).Returns(messageId);

        await sut.Handle(message, messageContext.Object);

        var jobAudit = dbContext.JobAudits.FirstOrDefault();

        
        Assert.Multiple(() =>
        {
            Assert.That(dbContext.AccountLegalEntities.Count, Is.EqualTo(1));
            Assert.That(dbContext.Accounts.Count, Is.EqualTo(1));
            Assert.That(jobAudit, Is.Not.Null);
            var info = JsonSerializer.Deserialize<EventHandlerJobInfo<AddedLegalEntityEvent>>(jobAudit!.JobInfo!)!;
            Assert.That(jobAudit.JobName, Is.EqualTo(nameof(AddedLegalEntityEventHandler)));
            Assert.That(info.IsSuccess, Is.True);
        });
    }

    [Test, AutoData]
    public async Task AddedLegalEntityEventHandlerTests_Handle_AccountLegalEntityDoesNotExists_CreatesAccountLegalEntity(AddedLegalEntityEvent message, string messageId)
    {
        using var dbContext = DbContextHelper.CreateInMemoryDbContext()
            .AddAccount(AccountData.Create(message.AccountId))
            .PersistChanges();

        IEmployerAccountsApiClient employerAccountsClient = Mock.Of<IEmployerAccountsApiClient>();

        AddedLegalEntityEventHandler sut = new(Mock.Of<ILogger<AddedLegalEntityEventHandler>>(), dbContext, employerAccountsClient);

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
