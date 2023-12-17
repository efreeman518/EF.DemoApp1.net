using Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configuration;

public class TodoItemConfiguration : EntityBaseConfiguration<TodoItem>
{
    public override void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        base.Configure(builder);
        builder.ToTable("TodoItem")
            .HasIndex(i => i.Name).IsUnique().IsClustered();

        //entities can have only a single query filter, but can be compound
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(b => b.Name).HasMaxLength(100);
        builder.Property(b => b.SecureDeterministic).HasMaxLength(100);
        builder.Property(b => b.SecureRandom).HasMaxLength(100);
    }
}
