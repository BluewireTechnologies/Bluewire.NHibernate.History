using System.Collections.Generic;
using Bluewire.IntervalTree;
using Bluewire.NHibernate.Audit.Attributes;
using Bluewire.NHibernate.Audit.UnitTests.OneToMany.Entity;

namespace Bluewire.NHibernate.Audit.UnitTests.IntervalTree
{
    [AuditableEntity(typeof(EntityWithListOfEntityTypesWithIntervalAuditHistory))]
    public class EntityWithListOfEntityTypesWithInterval
    {
        public EntityWithListOfEntityTypesWithInterval()
        {
            Entities = new List<OneToManyEntity>();
        }

        public virtual int Id { get; set; }

        [AuditableRelation(typeof(EntityWithListOfEntityTypesWithIntervalEntitiesAuditHistory))]
        public virtual IList<OneToManyEntity> Entities { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithListOfEntityTypesWithIntervalAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithListOfEntityTypesWithIntervalEntitiesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, int>
    {
        [AuditInterval(typeof(PerMinuteSnapshotIntervalTree32))]
        public virtual RitEntry32 RitMinutes { get; set; }
    }
}
