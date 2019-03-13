using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;
using Bluewire.NHibernate.Audit.UnitTests.ManyToMany;

namespace Bluewire.NHibernate.Audit.UnitTests.Simple
{
    [AuditableEntity(typeof(EntityWithAuditedNonPublicCollectionAuditHistory))]
    public class EntityWithAuditedNonPublicCollection
    {
        public EntityWithAuditedNonPublicCollection()
        {
            Entities_NonPublic = new List<ReferencableEntity>();
        }

        public virtual int Id { get; set; }
        public virtual string Value { get; set; }
        public virtual int VersionId { get; set; }

        [AuditableRelation(typeof(EntityWithAuditedNonPublicCollectionEntitiesAuditHistory))]
        protected virtual IList<ReferencableEntity> Entities_NonPublic { get; set; }

        public virtual IList<ReferencableEntity> Entities => Entities_NonPublic;
    }

    public class EntityWithAuditedNonPublicCollectionAuditHistory : EntityAuditHistoryBase<int, int>
    {
        public virtual string Value { get; set; }
    }

    public class EntityWithAuditedNonPublicCollectionEntitiesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, int>
    {
    }
}
