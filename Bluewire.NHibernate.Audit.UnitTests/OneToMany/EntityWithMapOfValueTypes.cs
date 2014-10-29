using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany
{
    [AuditableEntity(typeof(EntityWithMapOfValueTypesAuditHistory))]
    public class EntityWithMapOfValueTypes
    {
        public EntityWithMapOfValueTypes()
        {
            Values = new Dictionary<string, ComponentType>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithMapOfValueTypesValuesAuditHistory), "EntityWithMapOfValueTypesId", "Key")]
        public virtual IDictionary<string, ComponentType> Values { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithMapOfValueTypesAuditHistory : IAuditHistory
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

    public class EntityWithMapOfValueTypesValuesAuditHistory : IKeyedRelationAuditHistory
    {
        public virtual int EntityWithMapOfValueTypesId { get; set; }

        public virtual string Key { get; protected set; }
        public virtual string String { get; set; }
        public virtual int Integer { get; set; }

        public virtual long AuditId { get; protected set; }
        public virtual DateTimeOffset StartDatestamp { get; set; }
        public virtual DateTimeOffset? EndDatestamp { get; set; }

        object IKeyedRelationAuditHistory.Key
        {
            get { return Key; }
            set { Key = (string)value; }
        }
    }
}
