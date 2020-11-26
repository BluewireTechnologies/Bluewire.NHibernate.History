using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Component
{
    [AuditableEntity(typeof(EntityWithIdBagOfValueTypesAuditHistory))]
    public class EntityWithIdBagOfValueTypes
    {
        public EntityWithIdBagOfValueTypes()
        {
            Values = new List<ComponentType>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithIdBagOfValueTypesValuesAuditHistory))]
        public virtual ICollection<ComponentType> Values { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithIdBagOfValueTypesAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithIdBagOfValueTypesValuesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, ComponentType>
    {
    }
}
