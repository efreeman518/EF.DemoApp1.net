namespace Package.Infrastructure.Common;

public interface ISpecification<in T>
{
    bool IsSatisfiedBy(T t);
}

public class Specification<T>(Func<T, bool> predicate) : ISpecification<T> where T : class
{
    private readonly Func<T, bool> _predicate = predicate;

    public virtual bool IsSatisfiedBy(T t) { return _predicate(t); }

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
    { return spec._predicate; }
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
