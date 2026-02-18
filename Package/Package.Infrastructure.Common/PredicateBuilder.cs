using System.Linq.Expressions;

namespace Package.Infrastructure.Common;

//http://www.albahari.com/nutshell/predicatebuilder.aspx

public static class PredicateBuilder
{
    public static Expression<Func<T, bool>> True<T>() { return f => true; }
    public static Expression<Func<T, bool>> False<T>() { return f => false; }

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1,
                                                        Expression<Func<T, bool>> expr2)
    {
        var secondBody = ReplaceParameter(expr2, expr1.Parameters[0]);
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, secondBody), expr1.Parameters);
    }

    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
                                                         Expression<Func<T, bool>> expr2)
    {
        var secondBody = ReplaceParameter(expr2, expr1.Parameters[0]);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, secondBody), expr1.Parameters);
    }

    private static Expression ReplaceParameter<T>(Expression<Func<T, bool>> expression, ParameterExpression targetParameter)
    {
        return new ReplaceParameterVisitor(expression.Parameters[0], targetParameter).Visit(expression.Body)!;
    }

    private sealed class ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == source ? target : base.VisitParameter(node);
        }
    }
}