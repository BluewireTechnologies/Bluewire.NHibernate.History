using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Component
{
    [AuditableEntity(typeof(EntityWithListOfValueTypesAuditHistory))]
    public class EntityWithListOfValueTypes
    {
        public EntityWithListOfValueTypes()
        {
            Values = new List<ComponentType>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithListOfValueTypesValuesAuditHistory))]
        public virtual IList<ComponentType> Values { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithListOfValueTypesAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithListOfValueTypesValuesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, ComponentType>
    {
    }
}
