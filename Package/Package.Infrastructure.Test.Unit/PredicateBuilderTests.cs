using System.Linq.Expressions;
using Package.Infrastructure.Common;

namespace Package.Infrastructure.Test.Unit;

[TestClass]
public class PredicateBuilderTests
{
    [TestMethod]
    public void And_composition_is_correct_and_contains_no_invoke_nodes()
    {
        Expression<Func<int, bool>> greaterThanFive = x => x > 5;
        Expression<Func<int, bool>> even = y => y % 2 == 0;

        var combined = greaterThanFive.And(even);
        var values = new[] { 2, 5, 6, 7, 8, 11 };
        var filtered = values.AsQueryable().Where(combined).ToList();

        CollectionAssert.AreEqual(new List<int> { 6, 8 }, filtered);
        Assert.IsFalse(ContainsInvocationExpression(combined));
    }

    [TestMethod]
    public void Or_composition_is_correct_and_contains_no_invoke_nodes()
    {
        Expression<Func<int, bool>> lessThanThree = x => x < 3;
        Expression<Func<int, bool>> greaterThanTen = y => y > 10;

        var combined = lessThanThree.Or(greaterThanTen);
        var values = new[] { 1, 2, 3, 10, 11, 12 };
        var filtered = values.AsQueryable().Where(combined).ToList();

        CollectionAssert.AreEqual(new List<int> { 1, 2, 11, 12 }, filtered);
        Assert.IsFalse(ContainsInvocationExpression(combined));
    }

    private static bool ContainsInvocationExpression(Expression expression)
    {
        var finder = new InvocationFinder();
        finder.Visit(expression);
        return finder.Found;
    }

    private sealed class InvocationFinder : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            Found = true;
            return base.VisitInvocation(node);
        }
    }
}