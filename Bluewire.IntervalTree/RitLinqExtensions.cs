using System;
using System.Linq;
using System.Linq.Expressions;

namespace Bluewire.IntervalTree
{
    public static class RitLinqExtensions
    {
        public static IQueryable<T> Overlapping<T>(this IQueryable<T> queryable, Expression<Func<T, RitEntry32>> selectProperty, RitQuery32 query)
        {
            return queryable.Where(query.CreateFilterExpression(selectProperty));
        }

        private static readonly RitExpressionBuilder ritExpressionBuilder = new RitExpressionBuilder();

        public static Expression<Func<T, bool>> CreateFilterExpression<T>(this RitQuery32 query, Expression<Func<T, RitEntry32>> selectProperty)
        {
            return ritExpressionBuilder.CreateFilterExpression(query, selectProperty);
        }

        public static Expression<Func<RitEntry32, bool>> ToFilterExpression(this RitQuery32 query)
        {
            return ritExpressionBuilder.CreateFilterExpression(query);
        }

        public static Func<RitEntry32, bool> ToFilter(this RitQuery32 query)
        {
            return entry => entry.Node != null && (
                (query.LeftNodes.Any(n => n == entry.Node) && entry.Upper >= query.Lower)
                || (entry.Node >= query.Lower && entry.Node <= query.Upper)
                || (query.RightNodes.Any(n => n == entry.Node) && entry.Lower <= query.Upper)
                );
        }
    }
}
