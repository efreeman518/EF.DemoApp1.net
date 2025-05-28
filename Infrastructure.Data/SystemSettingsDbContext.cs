﻿using Domain.Model;
using Microsoft.EntityFrameworkCore;
using Package.Infrastructure.Data;
using ZLinq;

namespace Infrastructure.Data;

public class SystemSettingsDbContext(DbContextOptions<SystemSettingsDbContext> options) : DbContextBase<string,Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //schema
        modelBuilder.HasDefaultSchema("todo");

        //datatype defaults for sql
        //decimal
        foreach (var property in modelBuilder.Model.GetEntityTypes().AsValueEnumerable()
            .SelectMany(t => t.GetProperties().AsValueEnumerable())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            if (property.GetColumnType() == null) property.SetColumnType("decimal(10,4)");
        }
        //dates
        foreach (var property in modelBuilder.Model.GetEntityTypes().AsValueEnumerable()
            .SelectMany(t => t.GetProperties().AsValueEnumerable())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            if (property.GetColumnType() == null) property.SetColumnType("datetime2");
        }

        //table configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SystemSettingsDbContext).Assembly);
    }

    //DbSets
    public DbSet<SystemSetting> SystemSettings { get; set; } = null!;
}
