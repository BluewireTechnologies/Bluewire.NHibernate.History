using System.Collections.Generic;
using Bluewire.IntervalTree;
using Bluewire.NHibernate.Audit.Attributes;
using Bluewire.NHibernate.Audit.UnitTests.OneToMany.Entity;

namespace Bluewire.NHibernate.Audit.UnitTests.IntervalTree
{
    [AuditableEntity(typeof(EntityWithSetOfEntityTypesWithIntervalAuditHistory))]
    public class EntityWithSetOfEntityTypesWithInterval
    {
        public EntityWithSetOfEntityTypesWithInterval()
        {
            Entities = new HashSet<OneToManyEntity>();
        }

        public virtual int Id { get; set; }

        [AuditableRelation(typeof(EntityWithSetOfEntityTypesWithIntervalEntitiesAuditHistory))]
        public virtual ICollection<OneToManyEntity> Entities { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithSetOfEntityTypesWithIntervalAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithSetOfEntityTypesWithIntervalEntitiesAuditHistory : SetRelationAuditHistoryEntry<int, int>
    {
        [AuditInterval(typeof(PerMinuteSnapshotIntervalTree32))]
        public virtual RitEntry32 RitMinutes { get; set; }
    }
}
