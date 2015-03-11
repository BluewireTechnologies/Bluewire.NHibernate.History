using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Element
{
    [AuditableEntity(typeof(EntityWithListOfPrimitiveTypesAuditHistory))]
    public class EntityWithListOfPrimitiveTypes
    {
        public EntityWithListOfPrimitiveTypes()
        {
            Values = new List<string>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithListOfPrimitiveTypesValuesAuditHistory))]
        public virtual IList<string> Values { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithListOfPrimitiveTypesAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithListOfPrimitiveTypesValuesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, string>
    {
    }
}
