using System;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests
{
    [AuditableEntity(typeof(VersionedSimpleEntityAuditHistory))]
    public class VersionedSimpleEntity
    {
        public virtual int Id { get; set; }
        public virtual int VersionId { get; set; }
    }

    public class VersionedSimpleEntityAuditHistory : IAuditHistory
    {
        public virtual int Id { get; protected set; }
        public virtual int VersionId { get; protected set; }

        object IAuditHistory.VersionId
        {
            get { return VersionId; }
        }

        object IAuditHistory.Id
        {
            get { return Id; }
        }

        public virtual object PreviousVersionId { get; protected set; }

        public virtual DateTimeOffset Datestamp { get; protected set; }
        public virtual AuditedOperation AuditedOperation { get; protected set; }
    }
}
