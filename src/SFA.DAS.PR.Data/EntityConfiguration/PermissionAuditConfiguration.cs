using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.EntityConfiguration;

public sealed class PermissionAuditConfiguration : IEntityTypeConfiguration<PermissionsAudit>
{
    public void Configure(EntityTypeBuilder<PermissionsAudit> builder)
    {
        builder.HasKey(p => p.Id);
    }
}