using Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configuration;

public class SystemSettingConfiguration : AuditableBaseConfiguration<SystemSetting>
{
    public override void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        base.Configure(builder);
        builder.ToTable("SystemSetting")
            .HasIndex(i => i.Key).IsUnique().IsClustered();

        builder.Property(b => b.Key).HasMaxLength(100);
        builder.Property(b => b.Value).HasMaxLength(200);
    }
}
