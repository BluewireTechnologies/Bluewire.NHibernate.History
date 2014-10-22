using System;

namespace Bluewire.NHibernate.Audit
{
    public interface IAuditHistory
    {
        object VersionId { get; }
        object Id { get; }
        object PreviousVersionId { get; set; }
        DateTimeOffset AuditDatestamp { get; set; }
        AuditedOperation AuditedOperation { get; set; }
    }
}