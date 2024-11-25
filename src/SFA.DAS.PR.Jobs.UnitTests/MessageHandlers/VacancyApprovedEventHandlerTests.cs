using Esfa.Recruit.Vacancies.Client.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Infrastructure;
using SFA.DAS.PR.Jobs.MessageHandlers.Recruit;
using SFA.DAS.PR.Jobs.Models.Recruit;
using SFA.DAS.PR.Jobs.Services;

namespace SFA.DAS.PR.Tests.MessageHandlers.Recruit;

[TestFixture]
public class VacancyApprovedEventHandlerTests
{
    private Mock<ILogger<VacancyApprovedEventHandler>> _mockLogger;
    private Mock<IRecruitApiClient> _mockRecruitApiClient;
    private Mock<IProviderRelationshipsDataContext> _mockProviderRelationshipsDataContext;
    private Mock<IJobAuditRepository> _mockJobAuditRepository;
    private Mock<IRelationshipService> _mockRelationshipService;
    private VacancyApprovedEventHandler _sut;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<VacancyApprovedEventHandler>>();
        _mockRecruitApiClient = new Mock<IRecruitApiClient>();
        _mockProviderRelationshipsDataContext = new Mock<IProviderRelationshipsDataContext>();
        _mockJobAuditRepository = new Mock<IJobAuditRepository>();
        _mockRelationshipService = new Mock<IRelationshipService>();

        _sut = new VacancyApprovedEventHandler(
            _mockLogger.Object,
            _mockRecruitApiClient.Object,
            _mockProviderRelationshipsDataContext.Object,
            _mockJobAuditRepository.Object,
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
            .Returns(Task.CompletedTask);

        _mockJobAuditRepository
            .Setup(x => x.CreateJobAudit(It.IsAny<JobAudit>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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

        _mockJobAuditRepository.Verify(
            x => x.CreateJobAudit(
                It.Is<JobAudit>(audit =>
                    audit.JobName == nameof(VacancyApprovedEvent) &&
                    audit.JobInfo!.Contains("\"VacancyReference\":12345")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockProviderRelationshipsDataContext.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void Handle_ShouldThrowException_IfLiveVacancyIsNull()
    {
        var vacancyApprovedEvent = new VacancyApprovedEvent { VacancyReference = 12345 };

        _mockRecruitApiClient
            .Setup(x => x.GetLiveVacancy(vacancyApprovedEvent!.VacancyReference!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveVacancyModel?)null);

        var mockContext = new Mock<IMessageHandlerContext>();
        mockContext
            .SetupGet(x => x.CancellationToken)
            .Returns(CancellationToken.None);

        Func<Task> act = async () => await _sut.Handle(vacancyApprovedEvent, mockContext.Object);

        act.Should().ThrowAsync<Exception>();
    }
}
