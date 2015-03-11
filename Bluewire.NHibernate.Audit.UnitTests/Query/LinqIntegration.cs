using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bluewire.NHibernate.Audit.Attributes;
using Iesi.Collections.Generic;

namespace Bluewire.NHibernate.Audit.UnitTests.Query
{
    [AuditableEntity(typeof(EntityAuditHistory))]
    class Entity
    {
        public Entity()
        {
            List = new List<string>();
            Set = new HashedSet<string>();
            Map = new Dictionary<string, string>();
        }

        public virtual int Id { get; set; }

        [AuditableRelation(typeof(EntityListValuesAuditHistory))]
        public virtual IList<string> List { get; protected set; }
        [AuditableRelation(typeof(EntityMapValuesAuditHistory))]
        public virtual IDictionary<string, string> Map { get; protected set; }
        [AuditableRelation(typeof(EntitySetValuesAuditHistory))]
        public virtual Iesi.Collections.Generic.ISet<string> Set { get; protected set; }

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
