using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;
using Bluewire.NHibernate.Audit.UnitTests.ManyToMany;

namespace Bluewire.NHibernate.Audit.UnitTests.Simple
{
    [AuditableEntity(typeof(EntityWithUnauditedCollectionAuditHistory))]
    public class EntityWithUnauditedCollection
    {
        public EntityWithUnauditedCollection()
        {
            Entities = new List<ReferencableEntity>();
        }

        public virtual int Id { get; set; }
        public virtual string Value { get; set; }
        public virtual int VersionId { get; set; }

        public virtual IList<ReferencableEntity> Entities { get; protected set; }
    }

    public class EntityWithUnauditedCollectionAuditHistory : EntityAuditHistoryBase<int, int>
    {
        public virtual string Value { get; set; }
    }
}
