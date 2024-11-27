using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.CommitmentsV2.Messages.Events;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.MessageHandlers;
using SFA.DAS.PR.Jobs.OuterApi.Responses;
using SFA.DAS.PR.Jobs.Services;
using SFA.DAS.PR.Jobs.UnitTests;

namespace SFA.DAS.PR.Tests.MessageHandlers;

[TestFixture]
public class CohortAssignedToProviderEventHandlerTests
{
    private Mock<ILogger<CohortAssignedToProviderEventHandler>> _mockLogger;
    private Mock<ICommitmentsV2ApiClient> _mockCommitmentsV2ApiClient;
    private ProviderRelationshipsDataContext _context;
    private Mock<IRelationshipService> _mockRelationshipService;
    private CohortAssignedToProviderEventHandler _sut;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<CohortAssignedToProviderEventHandler>>();
        _mockCommitmentsV2ApiClient = new Mock<ICommitmentsV2ApiClient>();
        _mockRelationshipService = new Mock<IRelationshipService>();

        _context = DbContextHelper.CreateInMemoryDbContext();

        _sut = new CohortAssignedToProviderEventHandler(
            _mockLogger.Object,
            _mockCommitmentsV2ApiClient.Object,
            _context,
            _mockRelationshipService.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
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
            .Setup(x => x.CreateRelationship(
                It.IsAny<RelationshipModel>(),
                It.IsAny<CancellationToken>()
            )
        ).ReturnsAsync(true);

        var mockContext = new Mock<IMessageHandlerContext>();
        mockContext
            .SetupGet(x => x.CancellationToken)
            .Returns(CancellationToken.None);

        await _sut.Handle(message, mockContext.Object);

        _mockCommitmentsV2ApiClient.Verify(
            x => x.GetCohortDetails(message.CohortId, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockRelationshipService.Verify(
            x => x.CreateRelationship(
                It.Is<RelationshipModel>(model =>
                    model.AccountLegalEntityId == cohortDetails.AccountLegalEntityId &&
                    model.ProviderUkprn == cohortDetails.ProviderId &&
                    model.NotificationTemplateName == "LinkedAccountCohort"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _context.JobAudits.Should().HaveCount(1);
    }
}
