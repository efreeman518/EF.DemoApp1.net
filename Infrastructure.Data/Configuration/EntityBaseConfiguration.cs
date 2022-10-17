using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Package.Infrastructure.Data;

namespace Infrastructure.Data.Configuration;

public abstract class EntityBaseConfiguration<T> : IEntityTypeConfiguration<T> where T : EntityBase
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        //Guid PK
        builder.HasKey(c => c.Id).IsClustered(false);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(b => b.CreatedBy).HasMaxLength(100);
        builder.Property(b => b.UpdatedBy).HasMaxLength(100);
        builder.Property(b => b.CreatedDate).HasColumnType("datetime2(0)");
        builder.Property(b => b.UpdatedDate).HasColumnType("datetime2(0)");
    }
}
