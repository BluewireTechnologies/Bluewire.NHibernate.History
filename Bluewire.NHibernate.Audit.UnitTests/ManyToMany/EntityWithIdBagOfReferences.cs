using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.ManyToMany
{
    [AuditableEntity(typeof(EntityWithIdBagOfReferencesAuditHistory))]
    public class EntityWithIdBagOfReferences
    {
        public EntityWithIdBagOfReferences()
        {
            Entities = new List<ReferencableEntity>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithIdBagOfReferencesEntitiesAuditHistory))]
        public virtual ICollection<ReferencableEntity> Entities { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithIdBagOfReferencesAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithIdBagOfReferencesEntitiesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, int>
    {
    }
}
