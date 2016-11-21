using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Bluewire.IntervalTree
{
    public class RitExpressionBuilder
    {
        private readonly MethodInfo collectionContainsMethod = typeof(ICollection<int>).GetMethod("Contains");

        public Expression<Func<T, bool>> CreateFilterExpression<T>(RitQuery32 query, Expression<Func<T, RitEntry32>> selectProperty)
        {
            var parameter = selectProperty.Parameters[0];
            var ritProperty = selectProperty.Body;

            var body = CreateFilterBody(query, ritProperty);

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        public Expression<Func<RitEntry32, bool>> CreateFilterExpression(RitQuery32 query)
        {
            var parameter = Expression.Parameter(typeof(RitEntry32), "e");

            var body = CreateFilterBody(query, parameter);

            return Expression.Lambda<Func<RitEntry32, bool>>(body, parameter);
        }

        private BinaryExpression CreateFilterBody(RitQuery32 query, Expression ritProperty)
        {
            var nodeProperty = Expression.Property(ritProperty, "Node");
            var nodeValueProperty = Expression.Property(nodeProperty, "Value");
            var lowerProperty = Expression.Property(ritProperty, "Lower");
            var upperProperty = Expression.Property(ritProperty, "Upper");


            var notNullPredicate = Expression.NotEqual(nodeProperty, Expression.Constant(null));

            var leftPredicate = Expression.AndAlso(
                Expression.Call(Expression.Constant(query.LeftNodes), collectionContainsMethod, nodeValueProperty),
                Expression.GreaterThanOrEqual(upperProperty, Expression.Constant(query.Lower)));

            var middlePredicate = Expression.AndAlso(
                Expression.GreaterThanOrEqual(nodeValueProperty, Expression.Constant(query.Lower)),
                Expression.LessThanOrEqual(nodeValueProperty, Expression.Constant(query.Upper)));

            var rightPredicate = Expression.AndAlso(
                Expression.Call(Expression.Constant(query.RightNodes), collectionContainsMethod, nodeValueProperty),
                Expression.LessThanOrEqual(lowerProperty, Expression.Constant(query.Upper)));


            var body = Expression.AndAlso(
                notNullPredicate,
                Expression.OrElse(
                    leftPredicate,
                    Expression.OrElse(
                        middlePredicate,
                        rightPredicate)));
            return body;
        }
    }
}
