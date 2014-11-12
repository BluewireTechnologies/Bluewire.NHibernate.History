using System;
using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany
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

    public class EntityWithListOfPrimitiveTypesAuditHistory : IAuditHistory
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

    public class EntityWithListOfPrimitiveTypesValuesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, string>
    {
    }
}
