using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Bluewire.NHibernate.Audit.Support
{
    public static class CollectionHelpers
    {
        public static IList<T> RehydrateListWithPossibleGaps<T>(IEnumerable<KeyValuePair<int, T>> relations)
        {
            return relations.OrderBy(r => r.Key)
                .Aggregate(new List<T>(), (l, r) =>
                {
                    while (l.Count < r.Key) l.Add(default(T));
                    l.Add(r.Value);
                    return l;
                });
        }
    }
}
