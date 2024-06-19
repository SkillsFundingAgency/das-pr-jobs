using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.PR.Data.Entities;
using SFA.DAS.PR.Data.EntityConfiguration;

namespace SFA.DAS.PR.Data;

[ExcludeFromCodeCoverage]
public class ProviderRelationshipsDataContext : DbContext, IProviderRelationshipsDataContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountLegalEntity> AccountLegalEntities => Set<AccountLegalEntity>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<AccountProvider> AccountProviders => Set<AccountProvider>();
    public DbSet<AccountProviderLegalEntity> AccountProviderLegalEntities => Set<AccountProviderLegalEntity>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<JobAudit> JobAudits => Set<JobAudit>();

    public ProviderRelationshipsDataContext(DbContextOptions<ProviderRelationshipsDataContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProviderConfiguration).Assembly);
    }
}
