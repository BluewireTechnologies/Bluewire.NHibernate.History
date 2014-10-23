using System;

namespace Bluewire.NHibernate.Audit
{
    public interface IAuditHistory
    {
        /// <summary>
        /// Primary key of the audit table.
        /// </summary>
        long AuditId { get; }
        object VersionId { get; set; }
        object Id { get; }
        object PreviousVersionId { get; set; }
        DateTimeOffset AuditDatestamp { get; set; }
        AuditedOperation AuditedOperation { get; set; }
    }

    public abstract class ListElementAuditHistory
    {
        public virtual long AuditId { get; protected set; }
        public virtual DateTimeOffset StartDatestamp { get; set; }
        public virtual DateTimeOffset? EndDatestamp { get; set; }
        public virtual int Index { get; set; }
    }
}