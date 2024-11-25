using FluentAssertions;
using SFA.DAS.PR.Jobs.Services;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.MessageHandlers;
using SFA.DAS.PR.Jobs.OuterApi.Responses;

namespace SFA.DAS.PR.Tests.MessageHandlers;

[TestFixture]
public class CohortAssignedToProviderEventHandlerTests
{
    private Mock<ILogger<CohortAssignedToProviderEventHandler>> _mockLogger;
    private Mock<ICommitmentsV2ApiClient> _mockCommitmentsV2ApiClient;
    private Mock<IProviderRelationshipsDataContext> _mockProviderRelationshipsDataContext;
    private Mock<IRelationshipService> _mockRelationshipService;
    private Mock<IJobAuditRepository> _mockJobAuditRepository;
    private CohortAssignedToProviderEventHandler _sut;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<CohortAssignedToProviderEventHandler>>();
        _mockCommitmentsV2ApiClient = new Mock<ICommitmentsV2ApiClient>();
        _mockProviderRelationshipsDataContext = new Mock<IProviderRelationshipsDataContext>();
        _mockRelationshipService = new Mock<IRelationshipService>();
        _mockJobAuditRepository = new Mock<IJobAuditRepository>();

        _sut = new CohortAssignedToProviderEventHandler(
            _mockLogger.Object,
            _mockCommitmentsV2ApiClient.Object,
            _mockProviderRelationshipsDataContext.Object,
            _mockRelationshipService.Object,
            _mockJobAuditRepository.Object);
    }

    [Test]
    public async Task Handle_ShouldProcessCohortAssignedToProviderEventSuccessfully()
    {
        var message = new CohortAssignedToProviderEvent(12345, DateTime.UtcNow);
        var cohortDetails = new CohortModel()
        {
            LegalEntityName = "Name",
            ProviderName = "ProviderName",
            AccountLegalEntityId = 67890,
            ProviderId = 12345678
        };

        _mockCommitmentsV2ApiClient
            .Setup(x => x.GetCohortDetails(message.CohortId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cohortDetails);

        _mockRelationshipService
            .Setup(x => x.CreateRelationship<CohortAssignedToProviderEventHandler>(
                _mockLogger.Object,
                It.IsAny<RelationshipModel>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockJobAuditRepository
            .Setup(x => x.CreateJobAudit(It.IsAny<JobAudit>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockContext = new Mock<IMessageHandlerContext>();
        mockContext
            .SetupGet(x => x.CancellationToken)
            .Returns(CancellationToken.None);

        await _sut.Handle(message, mockContext.Object);

        _mockCommitmentsV2ApiClient.Verify(
            x => x.GetCohortDetails(message.CohortId, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockRelationshipService.Verify(
            x => x.CreateRelationship<CohortAssignedToProviderEventHandler>(
                _mockLogger.Object,
                It.Is<RelationshipModel>(model =>
                    model.AccountLegalEntityId == cohortDetails.AccountLegalEntityId &&
                    model.ProviderUkprn == cohortDetails.ProviderId &&
                    model.NotificationTemplateName == "LinkedAccountCohort"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockJobAuditRepository.Verify(
            x => x.CreateJobAudit(
                It.Is<JobAudit>(audit =>
                    audit.JobName == nameof(CohortAssignedToProviderEvent) &&
                    audit.JobInfo!.Contains("\"CohortId\":12345")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockProviderRelationshipsDataContext.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void Handle_ShouldThrowException_IfCohortIsNull()
    {
        var message = new CohortAssignedToProviderEvent(12345, DateTime.UtcNow);

        _mockCommitmentsV2ApiClient
            .Setup(x => x.GetCohortDetails(message.CohortId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CohortModel)null);

        var mockContext = new Mock<IMessageHandlerContext>();
        mockContext
            .SetupGet(x => x.CancellationToken)
            .Returns(CancellationToken.None);

        Func<Task> act = async () => await _sut.Handle(message, mockContext.Object);

        act.Should().ThrowAsync<Exception>();
    }
}
