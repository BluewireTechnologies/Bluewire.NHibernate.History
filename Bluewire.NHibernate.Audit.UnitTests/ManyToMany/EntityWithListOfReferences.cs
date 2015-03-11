using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.ManyToMany
{
    [AuditableEntity(typeof(EntityWithListOfReferencesAuditHistory))]
    public class EntityWithListOfReferences
    {
        public EntityWithListOfReferences()
        {
            Entities = new List<ReferencableEntity>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithListOfReferencesEntitiesAuditHistory))]
        public virtual IList<ReferencableEntity> Entities { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithListOfReferencesAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithListOfReferencesEntitiesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, int>
    {
    }
}
