using System;
using System.Linq;
using Bluewire.NHibernate.Audit.Meta;
using NHibernate;
using NHibernate.Linq;

namespace Bluewire.NHibernate.Audit.Query
{
    public class SessionSnapshotContext : ISnapshotContext
    {
        private readonly ISession session;
        public DateTimeOffset SnapshotDatestamp { get; private set; }

        public SessionSnapshotContext(ISession session, DateTimeOffset snapshotDatestamp)
        {
            this.session = session;
            this.SnapshotDatestamp = snapshotDatestamp;
        }

        public IQueryable<T> QueryableAudit<T>() where T : IAuditRecord
        {
            return session.Query<T>();
        }
    }
}
