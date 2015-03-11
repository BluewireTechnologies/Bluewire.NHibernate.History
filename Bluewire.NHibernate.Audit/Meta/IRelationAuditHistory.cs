using System;

namespace Bluewire.NHibernate.Audit.Meta
{
    /// <summary>
    /// Super-interface for all relation audit entries. Do not implement this directly.
    /// </summary>
    public interface IRelationAuditHistory : IAuditRecord
    {
        DateTimeOffset StartDatestamp { get; set; }
        DateTimeOffset? EndDatestamp { get; set; }
        object OwnerId { get; set; }
        object Value { get; set; }
    }
}