using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Component
{
    [AuditableEntity(typeof(EntityWithBagOfValueTypesAuditHistory))]
    public class EntityWithBagOfValueTypes
    {
        public EntityWithBagOfValueTypes()
        {
            Values = new List<ComponentType>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithBagOfValueTypesValuesAuditHistory))]
        public virtual ICollection<ComponentType> Values { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithBagOfValueTypesAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithBagOfValueTypesValuesAuditHistory : SetRelationAuditHistoryEntry<int, ComponentType>
    {
    }
}
