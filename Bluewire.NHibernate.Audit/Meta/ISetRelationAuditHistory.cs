namespace Bluewire.NHibernate.Audit.Meta
{
    /// <summary>
    /// Identifies an audit entry for a set collection. Prefer extending SetRelationAuditHistoryEntry&lt;,&gt; over
    /// implementing this interface.
    /// </summary>
    public interface ISetRelationAuditHistory : IRelationAuditHistory
    {
    }
}
