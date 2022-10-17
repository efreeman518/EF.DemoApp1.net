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

        builder.Property(b => b.Name).HasMaxLength(100);
    }
}
