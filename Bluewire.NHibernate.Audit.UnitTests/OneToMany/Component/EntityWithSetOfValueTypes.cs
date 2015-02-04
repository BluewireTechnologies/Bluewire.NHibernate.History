using System;
using Bluewire.NHibernate.Audit.Attributes;
using Iesi.Collections.Generic;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Component
{
    [AuditableEntity(typeof(EntityWithSetOfValueTypesAuditHistory))]
    public class EntityWithSetOfValueTypes
    {
        public EntityWithSetOfValueTypes()
        {
            Values = new HashedSet<ComponentType>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithSetOfValueTypesValuesAuditHistory))]
        public virtual ISet<ComponentType> Values { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithSetOfValueTypesAuditHistory : IAuditHistory
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

        object IAuditHistory.PreviousVersionId
        {
            get { return PreviousVersionId; }
            set { PreviousVersionId = (int?)value; }
        }

        public virtual DateTimeOffset AuditDatestamp { get; set; }
        public virtual AuditedOperation AuditedOperation { get; set; }
    }

    public class EntityWithSetOfValueTypesValuesAuditHistory : SetRelationAuditHistoryEntry<int, ComponentType>
    {
    }
}
