﻿namespace Package.Infrastructure.Domain;

public abstract class EntityBase : IEntityBase<Guid>
{
    private readonly Guid _id = Guid.CreateVersion7();
    protected EntityBase()
    {
    }

    public Guid Id
    {
        get { return _id; }
        init { if (value != Guid.Empty) _id = value; }
    }

    //using as a shadow property causes concurrency problems when not tracked then attached, so keeping it on the base class, we always have it 
    public byte[]? RowVersion { get; set; }
}
