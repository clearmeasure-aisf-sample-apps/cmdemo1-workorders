using System.Linq.Expressions;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Expression-tree helpers for work-order predicates and projections.
/// </summary>
public static class WorkOrderExpression
{
    /// <summary>Rebinds <paramref name="expression"/> onto <paramref name="parameter"/>.</summary>
    public static Expression Rebind<T>(Expression<Func<WorkOrder, T>> expression, ParameterExpression parameter)
    {
        var body = new ParameterReplacer(expression.Parameters[0], parameter).Visit(expression.Body);
        ArgumentNullException.ThrowIfNull(body);
        return body;
    }

    /// <summary>Combines two work-order predicates with AndAlso, sharing one parameter.</summary>
    public static Expression<Func<WorkOrder, bool>> AndAlso(
        Expression<Func<WorkOrder, bool>> left,
        Expression<Func<WorkOrder, bool>> right) =>
        Combine(left, right, Expression.AndAlso);

    /// <summary>Combines two work-order predicates with OrElse, sharing one parameter.</summary>
    public static Expression<Func<WorkOrder, bool>> OrElse(
        Expression<Func<WorkOrder, bool>> left,
        Expression<Func<WorkOrder, bool>> right) =>
        Combine(left, right, Expression.OrElse);

    private static Expression<Func<WorkOrder, bool>> Combine(
        Expression<Func<WorkOrder, bool>> left,
        Expression<Func<WorkOrder, bool>> right,
        Func<Expression, Expression, BinaryExpression> combine)
    {
        var parameter = left.Parameters[0];
        return Expression.Lambda<Func<WorkOrder, bool>>(
            combine(left.Body, Rebind(right, parameter)), parameter);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}
