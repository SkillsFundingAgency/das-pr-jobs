using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SFA.DAS.PR.Data.Entities;

namespace SFA.DAS.PR.Data.EntityConfiguration;

[ExcludeFromCodeCoverage]
public class AccountLegalEntityConfiguration : IEntityTypeConfiguration<AccountLegalEntity>
{
    public void Configure(EntityTypeBuilder<AccountLegalEntity> builder)
    {
        builder.HasKey(p => p.Id);
    }
}