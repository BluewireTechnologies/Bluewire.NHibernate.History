using System;
using System.Linq;
using Bluewire.NHibernate.Audit.Meta;

namespace Bluewire.NHibernate.Audit.Query
{
    public interface ISnapshotContext
    {
        DateTimeOffset SnapshotDatestamp { get; }
        /// <summary>
        /// Begin a query against audit history.
        /// </summary>
        /// <remarks>
        /// This is a low-level method. Prefer the GetModel&lt;TEntity, TEntityKey&gt; extension method.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IQueryable<T> QueryableAudit<T>() where T : IAuditRecord;
    }
}
