using Esfa.Recruit.Vacancies.Client.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.MessageHandlers.Recruit;
using SFA.DAS.PR.Jobs.Models.Recruit;
using SFA.DAS.PR.Jobs.Services;
using SFA.DAS.PR.Jobs.UnitTests;

namespace SFA.DAS.PR.Tests.MessageHandlers.Recruit;

[TestFixture]
public class VacancyApprovedEventHandlerTests
{
    private Mock<ILogger<VacancyApprovedEventHandler>> _mockLogger;
    private Mock<IRecruitApiClient> _mockRecruitApiClient;
    private IProviderRelationshipsDataContext _context;
    private Mock<IRelationshipService> _mockRelationshipService;
    private VacancyApprovedEventHandler _sut;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<VacancyApprovedEventHandler>>();
        _mockRecruitApiClient = new Mock<IRecruitApiClient>();
        _mockRelationshipService = new Mock<IRelationshipService>();

        _context = DbContextHelper.CreateInMemoryDbContext();

        _sut = new VacancyApprovedEventHandler(
            _mockLogger.Object,
            _mockRecruitApiClient.Object,
            _context,
            _mockRelationshipService.Object);
    }

    [Test]
    public async Task Handle_ShouldProcessVacancyApprovedEventSuccessfully()
    {
        var vacancyApprovedEvent = new VacancyApprovedEvent { VacancyReference = 12345 };
        var liveVacancy = new LiveVacancyModel()
        {
            VacancyId = Guid.NewGuid(),
            AccountLegalEntityPublicHashedId = "ABC123",
            TrainingProvider = new TrainingProviderModel { Ukprn = 12345678 },
            AccountPublicHashedId = "ACC123"
        };

        _mockRecruitApiClient
            .Setup(x => x.GetLiveVacancy(vacancyApprovedEvent.VacancyReference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(liveVacancy);

        _mockRelationshipService
            .Setup(x => x.CreateRelationship(
                It.IsAny<RelationshipModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockContext = new Mock<IMessageHandlerContext>();
        mockContext
            .SetupGet(x => x.CancellationToken)
            .Returns(CancellationToken.None);

        await _sut.Handle(vacancyApprovedEvent, mockContext.Object);

        _mockRecruitApiClient.Verify(
            x => x.GetLiveVacancy(vacancyApprovedEvent.VacancyReference, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockRelationshipService.Verify(
            x => x.CreateRelationship(
                It.Is<RelationshipModel>(model =>
                    model.AccountLegalEntityPublicHashedId == liveVacancy.AccountLegalEntityPublicHashedId &&
                    model.ProviderUkprn == liveVacancy.TrainingProvider.Ukprn &&
                    model.NotificationTemplateName == "LinkedAccountRecruit"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _context.JobAudits.Should().HaveCount(1);
    }
}
