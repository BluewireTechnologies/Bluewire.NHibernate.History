using System;
using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.Query
{
    [AuditableEntity(typeof(EntityAuditHistory))]
    class Entity
    {
        public Entity()
        {
            List = new List<string>();
            Set = new HashSet<string>();
            Map = new Dictionary<string, string>();
        }

        public virtual int Id { get; set; }

        [AuditableRelation(typeof(EntityListValuesAuditHistory))]
        public virtual IList<string> List { get; protected set; }
        [AuditableRelation(typeof(EntityMapValuesAuditHistory))]
        public virtual IDictionary<string, string> Map { get; protected set; }
        [AuditableRelation(typeof(EntitySetValuesAuditHistory))]
        public virtual ISet<string> Set { get; protected set; }

        public virtual int VersionId { get; set; }
    }

    class EntityAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    class EntityListValuesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, string>
    {
    }

    class EntityMapValuesAuditHistory : KeyedRelationAuditHistoryEntry<int, string, string>
    {
    }

    class EntitySetValuesAuditHistory : SetRelationAuditHistoryEntry<int, string>
    {
    }
}
