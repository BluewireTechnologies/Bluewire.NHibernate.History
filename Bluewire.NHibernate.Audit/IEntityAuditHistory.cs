using System;
using Bluewire.NHibernate.Audit.Meta;

namespace Bluewire.NHibernate.Audit
{
    /// <summary>
    /// Interface for entity audit records.
    /// </summary>
    /// <remarks>
    /// It is suggested that all the 'object'-typed Id properties on this interface be
    /// implemented explicitly, delegating to strongly-typed properties. It may be convenient
    /// to define a common base class for all your entity audit records.
    /// </remarks>
    public interface IEntityAuditHistory : IAuditRecord
    {
        object VersionId { get; set; }
        object Id { get; }
        object PreviousVersionId { get; set; }
        DateTimeOffset AuditDatestamp { get; set; }
        AuditedOperation AuditedOperation { get; set; }
    }
}