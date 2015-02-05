namespace Bluewire.NHibernate.Audit.Meta
{
    public interface IAuditRecord
    {
        /// <summary>
        /// Primary key of the audit table.
        /// </summary>
        long AuditId { get; }
    }
}