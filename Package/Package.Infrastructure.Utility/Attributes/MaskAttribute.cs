using System;

namespace Package.Infrastructure.Utility.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property)]
public class MaskAttribute : Attribute
{
    private readonly string mask;
    private readonly string? matchPattern;

    public MaskAttribute(string Mask = "*", string? MatchPattern = null)
    {
        this.mask = Mask;
        this.matchPattern = MatchPattern;
    }

    public virtual string Mask
    {
        get { return mask; }
    }

    public virtual string? MatchPattern
    {
        get { return matchPattern; }
    }

}
