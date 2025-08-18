using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Package.Infrastructure.Common.Extensions;
/// <summary>
/// Extensions for building and composing Expression&lt;Func&lt;T,bool&gt;&gt; (and related) in an EF Core friendly (Invoke-free) way.
/// Keep pure expression tree generation here; IQueryable helpers belong in IQueryableExtensions.
/// </summary>
public static class ExpressionExtensions
{
    #region Seed predicates

    public static Expression<Func<T, bool>> True<T>() => static _ => true;
    public static Expression<Func<T, bool>> False<T>() => static _ => false;

    #endregion

    #region Core boolean composition

    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right) =>
        Compose(left, right, Expression.AndAlso);

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right) =>
        Compose(left, right, Expression.OrElse);

    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expr)
    {
        var p = expr.Parameters[0];
        return Expression.Lambda<Func<T, bool>>(Expression.Not(expr.Body), p);
    }

    /// <summary>
    /// Compose an arbitrary set of predicates with AND.
    /// </summary>
    public static Expression<Func<T, bool>> AndAll<T>(params Expression<Func<T, bool>>[] predicates) =>
        predicates is { Length: > 0 }
            ? predicates.Skip(1).Aggregate(predicates[0], (acc, next) => acc.And(next))
            : True<T>();

    /// <summary>
    /// Compose an arbitrary set of predicates with OR.
    /// </summary>
    public static Expression<Func<T, bool>> OrAny<T>(params Expression<Func<T, bool>>[] predicates) =>
        predicates is { Length: > 0 }
            ? predicates.Skip(1).Aggregate(predicates[0], (acc, next) => acc.Or(next))
            : False<T>();

    private static Expression<Func<T, bool>> Compose<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> merge)
    {
        if (left is null) return right;
        if (right is null) return left;

        var param = left.Parameters[0];
        var replacedRight = right.Body.ReplaceParameter(right.Parameters[0], param);
        return Expression.Lambda<Func<T, bool>>(merge(left.Body, replacedRight), param);
    }

    #endregion

    #region Conditional composition helpers

    public static Expression<Func<T, bool>> AndIf<T>(this Expression<Func<T, bool>> expr,
        bool condition,
        Expression<Func<T, bool>> next) =>
        condition ? expr.And(next) : expr;

    public static Expression<Func<T, bool>> OrIf<T>(this Expression<Func<T, bool>> expr,
        bool condition,
        Expression<Func<T, bool>> next) =>
        condition ? expr.Or(next) : expr;

    public static Expression<Func<T, bool>> AndIfNotNull<T, TValue>(this Expression<Func<T, bool>> expr,
        TValue? value,
        Func<TValue, Expression<Func<T, bool>>> builder) where TValue : class =>
        value is not null ? expr.And(builder(value)) : expr;

    public static Expression<Func<T, bool>> AndIfHasValue<T, TValue>(this Expression<Func<T, bool>> expr,
        TValue? value,
        Func<TValue, Expression<Func<T, bool>>> builder) where TValue : struct =>
        value.HasValue ? expr.And(builder(value.Value)) : expr;

    public static Expression<Func<T, bool>> AndIfNotEmpty<T>(this Expression<Func<T, bool>> expr,
        string? value,
        Func<string, Expression<Func<T, bool>>> builder) =>
        !string.IsNullOrWhiteSpace(value) ? expr.And(builder(value!)) : expr;

    public static Expression<Func<T, bool>> AndIfAny<T, TValue>(this Expression<Func<T, bool>> expr,
        IEnumerable<TValue>? values,
        Func<IEnumerable<TValue>, Expression<Func<T, bool>>> builder) =>
        values is { } seq && seq.Any() ? expr.And(builder(seq)) : expr;

    #endregion

    #region Property selection utilities

    /// <summary>
    /// Builds a lambda: (T x) => (object?)x.PropertyPath. Supports dotted paths.
    /// Useful for dynamic ordering when you only need an object-returning selector.
    /// </summary>
    public static Expression<Func<T, object?>> BuildObjectSelector<T>(string propertyPath)
    {
        var param = Expression.Parameter(typeof(T), "x");
        Expression body = param;
        foreach (var part in propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            body = Expression.PropertyOrField(body, part);
        }

        if (body.Type.IsValueType)
            body = Expression.Convert(body, typeof(object));

        return Expression.Lambda<Func<T, object?>>(body, param);
    }

    #endregion

    #region Set membership (IN / NOT IN)

    public static Expression<Func<T, bool>> In<T, TProp>(this Expression<Func<T, TProp>> property,
        IEnumerable<TProp>? values)
    {
        var materialized = values?.Distinct().ToArray() ?? Array.Empty<TProp>();
        if (materialized.Length == 0)
            return False<T>();

        var param = property.Parameters[0];
        var propBody = property.Body.ReplaceParameter(property.Parameters[0], param);
        // Enumerable.Contains(array, propBody)
        var call = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            new[] { typeof(TProp) },
            Expression.Constant(materialized),
            propBody);

        return Expression.Lambda<Func<T, bool>>(call, param);
    }

    public static Expression<Func<T, bool>> NotIn<T, TProp>(this Expression<Func<T, TProp>> property,
        IEnumerable<TProp>? values)
    {
        var inExpr = property.In(values);
        // If values empty In returns False -> Not(False)=True, which is correct semantics.
        return inExpr.Not();
    }

    #endregion

    #region Range / comparison helpers

    public static Expression<Func<T, bool>> Between<T, TProp>(this Expression<Func<T, TProp>> property,
        TProp? minInclusive,
        TProp? maxInclusive) where TProp : struct, IComparable<TProp>
    {
        var param = property.Parameters[0];
        var body = property.Body.ReplaceParameter(property.Parameters[0], param);

        Expression? predicate = null;

        if (minInclusive.HasValue)
        {
            var ge = Expression.GreaterThanOrEqual(body, Expression.Constant(minInclusive.Value));
            predicate = predicate is null ? ge : Expression.AndAlso(predicate, ge);
        }

        if (maxInclusive.HasValue)
        {
            var le = Expression.LessThanOrEqual(body, Expression.Constant(maxInclusive.Value));
            predicate = predicate is null ? le : Expression.AndAlso(predicate, le);
        }

        return predicate is null
            ? True<T>()
            : Expression.Lambda<Func<T, bool>>(predicate, param);
    }

    #endregion

    #region String helpers (null-safe, case-insensitive options)

    public static Expression<Func<T, bool>> EqualsCI<T>(this Expression<Func<T, string?>> property, string? value)
    {
        if (value == null) return False<T>();
        return property.BuildStringPredicate(value, (propExpr, constExpr) =>
            Expression.Equal(
                Expression.Call(propExpr, nameof(string.Equals), Type.EmptyTypes, constExpr, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                Expression.Constant(true)));
    }

    public static Expression<Func<T, bool>> ContainsCI<T>(this Expression<Func<T, string?>> property, string? value) =>
        property.BuildStringComparison(value, StringComparison.OrdinalIgnoreCase, nameof(string.Contains));

    public static Expression<Func<T, bool>> StartsWithCI<T>(this Expression<Func<T, string?>> property, string? value) =>
        property.BuildStringComparison(value, StringComparison.OrdinalIgnoreCase, nameof(string.StartsWith));

    public static Expression<Func<T, bool>> EndsWithCI<T>(this Expression<Func<T, string?>> property, string? value) =>
        property.BuildStringComparison(value, StringComparison.OrdinalIgnoreCase, nameof(string.EndsWith));

    private static Expression<Func<T, bool>> BuildStringComparison<T>(
        this Expression<Func<T, string?>> property,
        string? value,
        StringComparison comparison,
        string methodName)
    {
        if (string.IsNullOrEmpty(value)) return True<T>();

        var param = property.Parameters[0];
        var propBody = property.Body.ReplaceParameter(property.Parameters[0], param);

        var notNull = Expression.NotEqual(propBody, Expression.Constant(null, typeof(string)));

        var method = typeof(string).GetMethods()
            .First(m => m.Name == methodName &&
                        m.GetParameters().Length == 2 &&
                        m.GetParameters()[1].ParameterType == typeof(StringComparison));

        var call = Expression.Call(propBody, method, Expression.Constant(value), Expression.Constant(comparison));
        var and = Expression.AndAlso(notNull, call);
        return Expression.Lambda<Func<T, bool>>(and, param);
    }

    private static Expression<Func<T, bool>> BuildStringPredicate<T>(
        this Expression<Func<T, string?>> property,
        string value,
        Func<Expression, Expression, Expression> build)
    {
        var param = property.Parameters[0];
        var propBody = property.Body.ReplaceParameter(property.Parameters[0], param);
        var notNull = Expression.NotEqual(propBody, Expression.Constant(null, typeof(string)));
        var call = build(propBody, Expression.Constant(value));
        var and = Expression.AndAlso(notNull, call);
        return Expression.Lambda<Func<T, bool>>(and, param);
    }

    #endregion

    #region Parameter replacement utility (internal)

    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;
        public ReplaceParameterVisitor(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == _from ? _to : base.VisitParameter(node);
    }

    internal static Expression ReplaceParameter(this Expression body, ParameterExpression from, ParameterExpression to) =>
        new ReplaceParameterVisitor(from, to).Visit(body)!;

    #endregion
}