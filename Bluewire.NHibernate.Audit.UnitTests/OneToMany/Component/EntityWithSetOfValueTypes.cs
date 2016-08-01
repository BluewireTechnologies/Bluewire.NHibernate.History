using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Component
{
    [AuditableEntity(typeof(EntityWithSetOfValueTypesAuditHistory))]
    public class EntityWithSetOfValueTypes
    {
        public EntityWithSetOfValueTypes()
        {
            Values = new HashSet<ComponentType>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithSetOfValueTypesValuesAuditHistory))]
        public virtual ISet<ComponentType> Values { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithSetOfValueTypesAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithSetOfValueTypesValuesAuditHistory : SetRelationAuditHistoryEntry<int, ComponentType>
    {
    }
}
