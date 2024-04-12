using Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text;

namespace Infrastructure.Data.Configuration;

public class TodoItemConfiguration : AuditableBaseConfiguration<TodoItem>
{
    public override void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        base.Configure(builder);
        builder.ToTable("TodoItem")
            .HasIndex(i => i.Name).IsUnique().IsClustered();

        //entities can have only a single query filter, but can be compound
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(b => b.Name).HasMaxLength(100);

        builder.Property(b => b.SecureDeterministic)
            .HasMaxLength(200) //encrypted column size needs to be larger than the max string property value 
            .HasConversion(
                v => v != null ? Encoding.UTF8.GetBytes(v) : null,
                v => v != null ? Encoding.UTF8.GetString(v) : null)
            ;
        builder.Property(b => b.SecureRandom)
            .HasMaxLength(200) //encrypted column size needs to be larger than the max string property value  
            .HasConversion(
                v => v != null ? Encoding.UTF8.GetBytes(v) : null,
                v => v != null ? Encoding.UTF8.GetString(v) : null)
            ;
    }
}
