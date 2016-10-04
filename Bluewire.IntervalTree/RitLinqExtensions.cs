using System;
using System.Linq;
using System.Linq.Expressions;

namespace Bluewire.IntervalTree
{
    public static class RitLinqExtensions
    {
        public static Expression<Func<RitEntry32, bool>> ToFilterExpression(this RitQuery32 query)
        {
            return entry => entry.Node != null && (
                (query.LeftNodes.Any(n => n == entry.Node) && entry.Upper >= query.Lower)
                || (entry.Node >= query.Lower && entry.Node <= query.Upper)
                || (query.RightNodes.Any(n => n == entry.Node) && entry.Lower <= query.Upper)
                );
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
