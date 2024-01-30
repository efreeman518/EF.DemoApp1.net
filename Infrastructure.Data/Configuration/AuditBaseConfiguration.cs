using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Package.Infrastructure.Data.Contracts;

namespace Infrastructure.Data.Configuration;

public abstract class AuditableBaseConfiguration<T> : IEntityTypeConfiguration<T> where T : AuditableBase<string>
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.Property(e => e.CreatedBy).HasMaxLength(100);
        builder.Property(e => e.UpdatedBy).HasMaxLength(100);
        builder.Property(e => e.CreatedDate).HasColumnType("datetime2(0)");
        builder.Property(e => e.UpdatedDate).HasColumnType("datetime2(0)");
    }
}
