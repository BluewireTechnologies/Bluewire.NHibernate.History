using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.Simple
{
    [AuditableEntity(typeof(EntityWithAuditedInverseCollectionAuditHistory))]
    public class EntityWithAuditedInverseCollection
    {
        public EntityWithAuditedInverseCollection()
        {
            Entities = new List<InverseReferencableEntity>();
        }

        public virtual int Id { get; set; }
        public virtual string Value { get; set; }
        public virtual int VersionId { get; set; }

        public virtual IList<InverseReferencableEntity> Entities { get; protected set; }
    }

    public class InverseReferencableEntity
    {
        public virtual int Id { get; set; }
        public virtual string String { get; set; }
        public virtual int OwnerId { get; set; }
    }

    public class EntityWithAuditedInverseCollectionAuditHistory : EntityAuditHistoryBase<int, int>
    {
        public virtual string Value { get; set; }
    }
}
