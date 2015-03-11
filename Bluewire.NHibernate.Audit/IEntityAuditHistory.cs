using Bluewire.NHibernate.Audit.Meta;

namespace Bluewire.NHibernate.Audit
{
    /// <summary>
    /// Interface for entity audit records.
    /// </summary>
    /// <remarks>
    /// It is suggested that all the 'object'-typed Id properties on this interface be
    /// implemented explicitly, delegating to strongly-typed properties. It may be convenient
    /// to define a common base class for all your entity audit records. A simple base type
    /// has been provided: EntityAuditHistoryBase&lt;TId, TVersion&gt;
    /// </remarks>
    public interface IEntityAuditHistory<TId> : IEntityAuditHistory
    {
        new TId Id { get; }
    }
}