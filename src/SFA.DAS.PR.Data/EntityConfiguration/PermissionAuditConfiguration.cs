using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.EntityConfiguration;

public sealed class PermissionAuditConfiguration : IEntityTypeConfiguration<PermissionAudit>
{
    public void Configure(EntityTypeBuilder<PermissionAudit> builder)
    {
        builder.HasKey(p => p.Id);
    }
}