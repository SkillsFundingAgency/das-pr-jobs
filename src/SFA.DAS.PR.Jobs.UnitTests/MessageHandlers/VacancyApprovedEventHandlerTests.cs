using AutoFixture.NUnit3;
using Esfa.Recruit.Vacancies.Client.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Jobs.MessageHandlers.Recruit;
using SFA.DAS.PR.Jobs.Services;
using SFA.DAS.PR.Jobs.UnitTests;

namespace SFA.DAS.PR.Tests.MessageHandlers.Recruit;

[TestFixture]
public class VacancyApprovedEventHandlerTests
{
    private Mock<ILogger<VacancyApprovedEventHandler>> _mockLogger;
    private ProviderRelationshipsDataContext _context;
    private Mock<IRelationshipService> _mockRelationshipService;
    private VacancyApprovedEventHandler _sut;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<VacancyApprovedEventHandler>>();
        _mockRelationshipService = new Mock<IRelationshipService>();

        _context = DbContextHelper.CreateInMemoryDbContext();

        _sut = new VacancyApprovedEventHandler(
            _mockLogger.Object,
            _context,
            _mockRelationshipService.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test, AutoData]
    public async Task Handle_ShouldProcessVacancyApprovedEventSuccessfully(string accountLegalEntityPublicHashedId, long ukprn)
    {
        var vacancyApprovedEvent = new VacancyApprovedEvent { VacancyReference = 12345, AccountLegalEntityPublicHashedId = accountLegalEntityPublicHashedId, Ukprn = ukprn };

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

        _mockRelationshipService.Verify(
            x => x.CreateRelationship(
                It.Is<RelationshipModel>(model =>
                    model.AccountLegalEntityPublicHashedId == vacancyApprovedEvent.AccountLegalEntityPublicHashedId &&
                    model.ProviderUkprn == vacancyApprovedEvent.Ukprn &&
                    model.NotificationTemplateName == "LinkedAccountRecruit"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _context.JobAudits.Should().HaveCount(1);
    }
}
