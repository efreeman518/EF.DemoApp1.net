namespace Package.Infrastructure.Common.Contracts;

public interface ISpecification
{
    bool IsSatisfied();
}
public sealed class Specification(Func<bool> predicate) : ISpecification
{
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages => _messages;
    public void AddMessage(string message) { if (!string.IsNullOrWhiteSpace(message)) _messages.Add(message); }

    public bool IsSatisfied() => predicate();

    public static Specification operator &(Specification a, ISpecification b) => new(() => a.IsSatisfied() && b.IsSatisfied());
    public static Specification operator |(Specification a, ISpecification b) => new(() => a.IsSatisfied() || b.IsSatisfied());
    public static Specification operator !(Specification a) => new(() => !a.IsSatisfied());
}

public static class NonGenericSpecificationExtensions
{
    // Adapt a non-generic spec to a typed one by ignoring the input
    public static Specification<T> For<T>(this ISpecification spec) => new(_ => spec.IsSatisfied());

    // Adapt a typed spec to a non-generic one by supplying a value
    public static Specification ToNonGeneric<T>(this ISpecification<T> spec, T value) => new(() => spec.IsSatisfiedBy(value));
}

public interface ISpecification<in T>
{
    bool IsSatisfiedBy(T t);
}

public class Specification<T> : ISpecification<T> // value types supported
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

    // generic <-> generic operators
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

    // mixed generic <-> non-generic operators (result stays generic)
    public static Specification<T> operator &(Specification<T> me, ISpecification other)
    { return new Specification<T>(t => me.IsSatisfiedBy(t) && other.IsSatisfied()); }
    public static Specification<T> operator &(ISpecification other, Specification<T> me)
    { return new Specification<T>(t => other.IsSatisfied() && me.IsSatisfiedBy(t)); }
    public static Specification<T> operator |(Specification<T> me, ISpecification other)
    { return new Specification<T>(t => me.IsSatisfiedBy(t) || other.IsSatisfied()); }
    public static Specification<T> operator |(ISpecification other, Specification<T> me)
    { return new Specification<T>(t => other.IsSatisfied() || me.IsSatisfiedBy(t)); }

    public static implicit operator Predicate<T>(Specification<T> spec)
    { return spec.IsSatisfiedBy; }
    public static implicit operator Func<T, bool>(Specification<T> spec)
    {
        return spec._predicate ?? throw new InvalidCastException("Cannot implicitly convert a specification without a predicate to a Func<T, bool>.");
    }
}

public static class GenericSpecificationExtensions
{
    public static Specification<T> And<T>(this ISpecification<T> me, ISpecification<T> other)
    { return new Specification<T>(t => me.IsSatisfiedBy(t) && other.IsSatisfiedBy(t)); }

    public static Specification<T> Or<T>(this ISpecification<T> me, ISpecification<T> other)
    { return new Specification<T>(t => me.IsSatisfiedBy(t) || other.IsSatisfiedBy(t)); }

    public static Specification<T> Not<T>(this ISpecification<T> me)
    { return new Specification<T>(t => !me.IsSatisfiedBy(t)); }
}

// Mixed extensions for ergonomic chaining without adapters
public static class MixedSpecificationExtensions
{
    public static Specification<T> AndWith<T>(this ISpecification<T> me, ISpecification other)
    { return new Specification<T>(t => me.IsSatisfiedBy(t) && other.IsSatisfied()); }

    public static Specification<T> AndWith<T>(this ISpecification me, ISpecification<T> other)
    { return new Specification<T>(t => me.IsSatisfied() && other.IsSatisfiedBy(t)); }

    public static Specification<T> OrWith<T>(this ISpecification<T> me, ISpecification other)
    { return new Specification<T>(t => me.IsSatisfiedBy(t) || other.IsSatisfied()); }

    public static Specification<T> OrWith<T>(this ISpecification me, ISpecification<T> other)
    { return new Specification<T>(t => me.IsSatisfied() || other.IsSatisfiedBy(t)); }
}
