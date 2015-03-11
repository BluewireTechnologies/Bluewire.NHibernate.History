using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Component
{
    [AuditableEntity(typeof(EntityWithMapOfValueTypesAuditHistory))]
    public class EntityWithMapOfValueTypes
    {
        public EntityWithMapOfValueTypes()
        {
            Values = new Dictionary<string, ComponentType>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithMapOfValueTypesValuesAuditHistory))]
        public virtual IDictionary<string, ComponentType> Values { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithMapOfValueTypesAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithMapOfValueTypesValuesAuditHistory : KeyedRelationAuditHistoryEntry<int, string, ComponentType>
    {
    }
}
