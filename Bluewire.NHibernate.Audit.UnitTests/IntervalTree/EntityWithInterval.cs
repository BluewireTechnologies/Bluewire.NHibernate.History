using Bluewire.IntervalTree;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.IntervalTree
{
    [AuditableEntity(typeof(EntityWithIntervalHistory))]
    public class EntityWithInterval
    {
        public virtual int Id { get; set; }
        public virtual string Value { get; set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithIntervalHistory: EntityAuditHistoryBase<int, int>
    {
        public virtual string Value { get; set; }

        [AuditInterval(typeof(PerMinuteSnapshotIntervalTree32))]
        public virtual RitEntry32 RitMinutes { get; set; }
    }
}
