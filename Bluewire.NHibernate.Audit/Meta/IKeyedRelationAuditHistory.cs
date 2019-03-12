namespace Bluewire.NHibernate.Audit.Meta
{
    /// <summary>
    /// Identifies an audit entry for a keyed collection. Prefer extending KeyedRelationAuditHistoryEntry&lt;,,&gt; over
    /// implementing this interface.
    /// </summary>
    public interface IKeyedRelationAuditHistory : IRelationAuditHistory
    {
        object Key { get; set; }
    }
}
