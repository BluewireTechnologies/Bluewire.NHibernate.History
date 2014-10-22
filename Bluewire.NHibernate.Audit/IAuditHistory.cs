using System;

namespace Bluewire.NHibernate.Audit
{
    public interface IAuditHistory
    {
        object VersionId { get; }
        object Id { get; }
        object PreviousVersionId { get; }
        DateTimeOffset Datestamp { get; }
        AuditedOperation AuditedOperation { get; }
    }
}