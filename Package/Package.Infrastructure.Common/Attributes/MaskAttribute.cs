namespace Package.Infrastructure.Common.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property)]
public class MaskAttribute(string Mask = "*", string? MatchPattern = null) : Attribute
{
    private readonly string mask = Mask;
    private readonly string? matchPattern = MatchPattern;

    public virtual string Mask
    {
        get { return mask; }
    }

    public virtual string? MatchPattern
    {
        get { return matchPattern; }
    }

}
