using System;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.ManyToOne
{
    public class UnauditedEntity
    {
        public virtual int Id { get; set; }
    }

    [AuditableEntity(typeof(EntityWithManyToOneAuditHistory))]
    public class EntityWithManyToOne
    {
        public virtual int Id { get; set; }
        public virtual UnauditedEntity Reference { get; set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithManyToOneAuditHistory : IEntityAuditHistory
    {
        public virtual int Id { get; protected set; }
        public virtual int? ReferenceId { get; set; }
        public virtual int? VersionId { get; protected set; }

        public virtual long AuditId { get; protected set; }
        public virtual int? PreviousVersionId { get; protected set; }

        object IEntityAuditHistory.VersionId
        {
            get { return VersionId; }
            set { VersionId = (int?)value; }
        }

        object IEntityAuditHistory.Id
        {
            get { return Id; }
        }

        object IEntityAuditHistory.PreviousVersionId
        {
            get { return PreviousVersionId; }
            set { PreviousVersionId = (int?)value; }
        }

        public virtual DateTimeOffset AuditDatestamp { get; set; }
        public virtual AuditedOperation AuditedOperation { get; set; }
    }
}
