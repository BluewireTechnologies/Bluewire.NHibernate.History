using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Entity
{
    [AuditableEntity(typeof(EntityWithSetOfEntityTypesAuditHistory))]
    public class EntityWithSetOfEntityTypes
    {
        public EntityWithSetOfEntityTypes()
        {
            Entities = new HashSet<OneToManyEntity>();
        }

        public virtual int Id { get; set; }

        [AuditableRelation(typeof(EntityWithSetOfEntityTypesEntitiesAuditHistory))]
        public virtual ISet<OneToManyEntity> Entities { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithSetOfEntityTypesAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithSetOfEntityTypesEntitiesAuditHistory : SetRelationAuditHistoryEntry<int, int>
    {
    }
}
