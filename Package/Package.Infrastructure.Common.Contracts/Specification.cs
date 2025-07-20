namespace Package.Infrastructure.Common.Contracts;

public interface ISpecification<in T>
{
    bool IsSatisfiedBy(T t);
}

public class Specification<T> : ISpecification<T> where T : class
{
    private readonly Func<T, bool>? _predicate;
    private readonly List<string> _messages = [];

    /// <summary>
    /// Constructor for creating a specification with a predicate.
    /// </summary>
    /// <param name="predicate">The predicate that defines the specification.</param>
    public Specification(Func<T, bool> predicate)
    {
        _predicate = predicate;
    }

    /// <summary>
    /// Constructor for creating a specification that will be implemented by a derived class.
    /// </summary>
    protected Specification() { }

    public IReadOnlyList<string> Messages => _messages;
    public void AddMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _messages.Add(message);
        }
    }

    public virtual bool IsSatisfiedBy(T t)
    {
        if (_predicate != null)
        {
            return _predicate(t);
        }
        // This will be overridden by derived classes that use the parameterless constructor.
        throw new NotImplementedException("Specification predicate not defined and IsSatisfiedBy not overridden.");
    }

    // optional
    public static Specification<T> operator &(Specification<T> me, ISpecification<T> other)
    { return me.And(other); }
    public static Specification<T> operator &(ISpecification<T> me, Specification<T> other)
    { return me.And(other); }
    public static Specification<T> operator &(Specification<T> me, Specification<T> other)
    { return me.And(other); }
    public static Specification<T> operator |(Specification<T> me, ISpecification<T> other)
    { return me.Or(other); }
    public static Specification<T> operator |(ISpecification<T> me, Specification<T> other)
    { return me.Or(other); }
    public static Specification<T> operator |(Specification<T> me, Specification<T> other)
    { return me.Or(other); }
    public static Specification<T> operator !(Specification<T> me)
    { return me.Not(); }

    public static implicit operator Predicate<T>(Specification<T> spec)
    { return spec.IsSatisfiedBy; }
    public static implicit operator Func<T, bool>(Specification<T> spec)
    {
        return spec._predicate ?? throw new InvalidCastException("Cannot implicitly convert a specification without a predicate to a Func<T, bool>.");
    }
}

public static class SpecificationExtensions
{
    public static Specification<T> And<T>(this ISpecification<T> me, ISpecification<T> other) where T : class
    { return new Specification<T>(t => me.IsSatisfiedBy(t) && other.IsSatisfiedBy(t)); }
    public static Specification<T> Or<T>(this ISpecification<T> me, ISpecification<T> other) where T : class
    { return new Specification<T>(t => me.IsSatisfiedBy(t) || other.IsSatisfiedBy(t)); }
    public static Specification<T> Not<T>(this ISpecification<T> me) where T : class
    { return new Specification<T>(t => !me.IsSatisfiedBy(t)); }
}
