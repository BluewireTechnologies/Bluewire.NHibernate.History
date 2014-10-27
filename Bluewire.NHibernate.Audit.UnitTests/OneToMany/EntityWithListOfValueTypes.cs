using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany
{
    [AuditableEntity(typeof(EntityWithListOfValueTypesAuditHistory))]
    public class EntityWithListOfValueTypes
    {
        public EntityWithListOfValueTypes()
        {
            Values = new List<ComponentType>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithListOfValueTypesValuesAuditHistory), "EntityWithListOfValueTypesId", "Index")]
        public virtual IList<ComponentType> Values { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithListOfValueTypesAuditHistory : IAuditHistory
    {
        public virtual int Id { get; set; }
        public virtual int? VersionId { get; set; }

        public virtual long AuditId { get; protected set; }
        public virtual int? PreviousVersionId { get; protected set; }

        object IAuditHistory.VersionId
        {
            get { return VersionId; }
            set { VersionId = (int?)value; }
        }

        object IAuditHistory.Id
        {
            get { return Id; }
        }

        object IAuditHistory.PreviousVersionId {
            get { return PreviousVersionId; }
            set { PreviousVersionId = (int?)value; }
        }

        public virtual DateTimeOffset AuditDatestamp { get; set; }
        public virtual AuditedOperation AuditedOperation { get; set; }
    }

    public class EntityWithListOfValueTypesValuesAuditHistory : IRelationAuditHistory
    {
        public virtual int EntityWithListOfValueTypesId { get; set; }

        public virtual int Index { get; protected set; }
        public virtual string String { get; set; }
        public virtual int Integer { get; set; }

        public virtual long AuditId { get; protected set; }
        public virtual DateTimeOffset StartDatestamp { get; set; }
        public virtual DateTimeOffset? EndDatestamp { get; set; }

        object IRelationAuditHistory.Key
        {
            get { return Index; }
            set { Index = (int)value; }
        }
    }
}
