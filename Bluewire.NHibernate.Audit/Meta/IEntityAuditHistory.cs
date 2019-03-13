using System;

namespace Bluewire.NHibernate.Audit.Meta
{
    /// <summary>
    /// Basemost interface for entity audit records. Do not implement this directly; prefer IEntityAuditHistory&lt;TId&gt;.
    /// </summary>
    public interface IEntityAuditHistory : IAuditRecord
    {
        object VersionId { get; set; }
        object Id { get; }
        object PreviousVersionId { get; set; }
        DateTimeOffset AuditDatestamp { get; set; }
        AuditedOperation AuditedOperation { get; set; }
    }
}
