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

    public interface IKeyedRelationAuditHistory
    {
        long AuditId { get;}
        DateTimeOffset StartDatestamp { get; set; }
        DateTimeOffset? EndDatestamp { get; set; }
        object Key { get; set; }
    }

    public interface ISetRelationAuditHistory
    {
        long AuditId { get; }
        DateTimeOffset StartDatestamp { get; set; }
        DateTimeOffset? EndDatestamp { get; set; }
    }
}