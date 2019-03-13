using System;
using System.Linq;
using Bluewire.NHibernate.Audit.Meta;
using Bluewire.NHibernate.Audit.Query;

namespace Bluewire.NHibernate.Audit.UnitTests.Query
{
    public class MockSnapshotContext : ISnapshotContext
    {
        private readonly IAuditRecord[] auditRecords;

        public MockSnapshotContext(DateTimeOffset snapshotDatestamp, params IAuditRecord[] auditRecords)
        {
            this.auditRecords = auditRecords;
            SnapshotDatestamp = snapshotDatestamp;
        }

        public DateTimeOffset SnapshotDatestamp { get; private set; }
        public IQueryable<T> QueryableAudit<T>() where T : IAuditRecord
        {
            return auditRecords.OfType<T>().AsQueryable<T>();
        }
    }
}
