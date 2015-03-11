using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Entity
{
    [AuditableEntity(typeof(EntityWithListOfEntityTypesAuditHistory))]
    public class EntityWithListOfEntityTypes
    {
        public EntityWithListOfEntityTypes()
        {
            Entities = new List<OneToManyEntity>();
        }

        public virtual int Id { get; set; }

        [AuditableRelation(typeof(EntityWithListOfEntityTypesEntitiesAuditHistory))]
        public virtual IList<OneToManyEntity> Entities { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithListOfEntityTypesAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithListOfEntityTypesEntitiesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, int>
    {
    }
}
