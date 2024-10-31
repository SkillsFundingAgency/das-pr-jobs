using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.DAS.PR.Data.Entities;
using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.PR.Data.EntityConfiguration;

[ExcludeFromCodeCoverage]
public sealed class PermissionsAuditConfiguration : IEntityTypeConfiguration<PermissionsAudit>
{
    public void Configure(EntityTypeBuilder<PermissionsAudit> builder)
    {
        builder.HasKey(p => p.Id);
    }
}
