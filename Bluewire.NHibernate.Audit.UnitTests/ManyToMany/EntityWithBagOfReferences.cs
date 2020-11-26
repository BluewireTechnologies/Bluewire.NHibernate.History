using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.ManyToMany
{
    [AuditableEntity(typeof(EntityWithBagOfReferencesAuditHistory))]
    public class EntityWithBagOfReferences
    {
        public EntityWithBagOfReferences()
        {
            Entities = new List<ReferencableEntity>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithBagOfReferencesEntitiesAuditHistory))]
        public virtual ICollection<ReferencableEntity> Entities { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithBagOfReferencesAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithBagOfReferencesEntitiesAuditHistory : SetRelationAuditHistoryEntry<int, int>
    {
    }
}
