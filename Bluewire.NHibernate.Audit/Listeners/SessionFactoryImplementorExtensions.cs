using System.Collections.Generic;
using System.Linq;
using NHibernate.Engine;
using NHibernate.Mapping;

namespace Bluewire.NHibernate.Audit.Listeners
{
    internal static class SessionFactoryImplementorExtensions
    {
        public static string[] ColumnNames(this ISessionFactoryImplementor factory, IEnumerable<ISelectable> columnIterator)
        {
            return columnIterator.Select(k => k.GetText(factory.Dialect)).ToArray();
        }

        public static string ColumnName(this ISessionFactoryImplementor factory, IEnumerable<ISelectable> columnIterator)
        {
            return columnIterator.Select(k => k.GetText(factory.Dialect)).Single();
        }
    }
}
