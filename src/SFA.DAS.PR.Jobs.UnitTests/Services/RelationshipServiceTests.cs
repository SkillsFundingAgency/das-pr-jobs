﻿using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.PR.Data;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.Repositories;
using SFA.DAS.PR.Jobs.Services;
using SFA.DAS.PR.Jobs.UnitTests;

namespace SFA.DAS.PR.Tests.Services;

[TestFixture]
public class RelationshipServiceTests
{
    private Mock<IAccountLegalEntityRepository> _mockAccountLegalEntityRepository;
    private Mock<IProvidersRepository> _mockProvidersRepository;
    private Mock<IAccountProviderRepository> _mockAccountProviderRepository;
    private ProviderRelationshipsDataContext _providerRelationshipsDataContext;
    private Mock<IAccountProviderLegalEntityRepository> _mockAccountProviderLegalEntityRepository;
    private Mock<IPermissionAuditRepository> _mockPermissionAuditRepository;
    private Mock<ILogger<RelationshipService>> _mockLogger;
    private RelationshipService sut;

    [TearDown]
    public void TearDown()
    {
        _providerRelationshipsDataContext.Dispose();
    }

    [SetUp]
    public void Setup()
    {
        _mockAccountLegalEntityRepository = new Mock<IAccountLegalEntityRepository>();
        _mockProvidersRepository = new Mock<IProvidersRepository>();
        _mockAccountProviderRepository = new Mock<IAccountProviderRepository>();
        _providerRelationshipsDataContext = DbContextHelper.CreateInMemoryDbContext();

        _mockAccountProviderLegalEntityRepository = new Mock<IAccountProviderLegalEntityRepository>();
        _mockPermissionAuditRepository = new Mock<IPermissionAuditRepository>();
        _mockLogger = new Mock<ILogger<RelationshipService>>();

        sut = new RelationshipService(
            _mockLogger.Object,
            _mockAccountLegalEntityRepository.Object,
            _mockProvidersRepository.Object,
            _mockAccountProviderRepository.Object,
            _providerRelationshipsDataContext,
            _mockAccountProviderLegalEntityRepository.Object,
            _mockPermissionAuditRepository.Object
        );
    }

    [Test]
    public async Task CreateRelationship_AccountLegalEntityIdAndAccountLegalEntityPublicHashedIdAreNull_ReturnsFalse()
    {
        var relationshipModel = new RelationshipModel(
            AccountLegalEntityId: null,
            AccountLegalEntityPublicHashedId: null,
            ProviderUkprn: 10000001,
            AccountPublicHashId: "accHash",
            NotificationTemplateName: "template",
            permissionAuditAction: nameof(PermissionAction.RecruitRelationship)
        );

        var actual = await sut.CreateRelationship(relationshipModel, CancellationToken.None);

        _mockAccountLegalEntityRepository.Verify(x => x.GetAccountLegalEntity(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockAccountLegalEntityRepository.Verify(x => x.GetAccountLegalEntity(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockProvidersRepository.Verify(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        actual.Should().BeFalse();
    }

    [Test]
    public async Task CreateRelationship_AccountLegalEntityNotFound_ReturnsFalse()
    {
        var relationshipModel = new RelationshipModel(
            AccountLegalEntityId: 1,
            AccountLegalEntityPublicHashedId: null,
            ProviderUkprn: 10000001,
            AccountPublicHashId: "AccHash",
            NotificationTemplateName: "template",
            permissionAuditAction: nameof(PermissionAction.RecruitRelationship));

        _mockAccountLegalEntityRepository
            .Setup(x => x.GetAccountLegalEntity(
                relationshipModel.AccountLegalEntityId!.Value,
                It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((AccountLegalEntity?)null);


        var actual = await sut.CreateRelationship(relationshipModel, CancellationToken.None);

        _mockAccountLegalEntityRepository.Verify(x =>
            x.GetAccountLegalEntity(relationshipModel.AccountLegalEntityId!.Value,
            It.IsAny<CancellationToken>()),
            Times.Once
        );

        _mockProvidersRepository.Verify(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        actual.Should().BeFalse();
    }

    [Test]
    public async Task CreateRelationship_ProviderNotFound_ReturnsFalse()
    {
        var accountLegalEntity = new AccountLegalEntity { Id = 1, AccountId = 100 };
        var relationshipModel = new RelationshipModel(
            AccountLegalEntityId: 1,
            AccountLegalEntityPublicHashedId: null,
            ProviderUkprn: 10000001,
            AccountPublicHashId: "acc123",
            NotificationTemplateName: "template",
            permissionAuditAction: "audit");

        Provider? provider = null;

        _mockAccountLegalEntityRepository
            .Setup(x => x.GetAccountLegalEntity(relationshipModel.AccountLegalEntityId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountLegalEntity);

        _mockProvidersRepository
            .Setup(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((long ukprn, CancellationToken token) => new ValueTask<Provider?>(provider));

        var actual = await sut.CreateRelationship(relationshipModel, CancellationToken.None);

        _mockAccountProviderRepository.Verify(x =>
            x.GetAccountProvider(
                relationshipModel.ProviderUkprn,
                accountLegalEntity.AccountId,
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );
        actual.Should().BeFalse();
    }

    [Test]
    public async Task CreateRelationship_NotFound_CreatesRelationship()
    {
        var accountLegalEntity = new AccountLegalEntity { Id = 1, AccountId = 100 };
        var provider = new Provider { Ukprn = 12345678 };
        var relationshipModel = new RelationshipModel(
            AccountLegalEntityId: 1,
            AccountLegalEntityPublicHashedId: null,
            ProviderUkprn: 12345678,
            AccountPublicHashId: "acc123",
            NotificationTemplateName: "template",
            permissionAuditAction: "audit"
        );

        _mockAccountLegalEntityRepository
            .Setup(x => x.GetAccountLegalEntity(relationshipModel.AccountLegalEntityId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountLegalEntity);

        _mockProvidersRepository
            .Setup(x => x.GetProvider(relationshipModel.ProviderUkprn, It.IsAny<CancellationToken>()))
            .Returns((long ukprn, CancellationToken token) => new ValueTask<Provider?>(provider));

        AccountProvider? accountProvider = null;
        _mockAccountProviderRepository
            .Setup(x => x.GetAccountProvider(relationshipModel.ProviderUkprn, accountLegalEntity.AccountId, It.IsAny<CancellationToken>()))
            .Returns((long providerUkprn, long accountId, CancellationToken token) => new ValueTask<AccountProvider?>(accountProvider));

        var actual = await sut.CreateRelationship(relationshipModel, CancellationToken.None);

        _mockPermissionAuditRepository.Verify(x =>
            x.CreatePermissionAudit(
                It.IsAny<PermissionsAudit>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        _mockAccountProviderLegalEntityRepository.Verify(a =>
            a.AddAccountProviderLegalEntity(
                It.IsAny<AccountProviderLegalEntity>()
            ),
            Times.Once
        );

        _providerRelationshipsDataContext.PersistChanges();
        _providerRelationshipsDataContext.Notifications.Should().HaveCount(1);
        _providerRelationshipsDataContext.Notifications.First().AccountLegalEntityId.Should().Be(accountLegalEntity.Id);
        actual.Should().BeTrue();
    }

    [Test]
    public async Task CreateRelationship_IfAccountProviderLegalEntityExists_ShouldNotCreateRelationshipAndReturnFalse()
    {
        var accountLegalEntity = new AccountLegalEntity { Id = 1, AccountId = 100 };
        var provider = new Provider { Ukprn = 12345678 };
        var accountProvider = new AccountProvider { Id = 1, AccountId = 100, ProviderUkprn = 12345678 };
        var accountProviderLegalEntity = new AccountProviderLegalEntity { Id = 1 };
        var relationshipModel = new RelationshipModel(
            AccountLegalEntityId: 1,
            AccountLegalEntityPublicHashedId: null,
            ProviderUkprn: 12345678,
            AccountPublicHashId: "acc123",
            NotificationTemplateName: "template",
            permissionAuditAction: "audit");

        _mockAccountLegalEntityRepository
            .Setup(x => x.GetAccountLegalEntity(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountLegalEntity);

        _mockProvidersRepository
             .Setup(x => x.GetProvider(It.IsAny<long>(), It.IsAny<CancellationToken>()))
             .Returns((long ukprn, CancellationToken token) => new ValueTask<Provider?>(provider));

        _mockAccountProviderRepository
            .Setup(x => x.GetAccountProvider(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns((long providerUkprn, long accountId, CancellationToken token) => new ValueTask<AccountProvider?>(accountProvider));

        _mockAccountProviderLegalEntityRepository
            .Setup(x => x.GetAccountProviderLegalEntity(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountProviderLegalEntity);

        var actual = await sut.CreateRelationship(relationshipModel, CancellationToken.None);

        _mockPermissionAuditRepository.Verify(x =>
            x.CreatePermissionAudit(
                It.IsAny<PermissionsAudit>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Never
        );

        _mockAccountProviderLegalEntityRepository.Verify(a =>
            a.AddAccountProviderLegalEntity(
                It.IsAny<AccountProviderLegalEntity>()
            ),
            Times.Never
        );
        actual.Should().BeFalse();
    }
}
