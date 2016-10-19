using System;

namespace Bluewire.NHibernate.Audit.Model
{
    public interface IAuditableRelationModel : IAuditRecordModel
    {
        string CollectionRole { get; }
        /// <summary>
        /// The type of value stored against each audited change. Usually the same as the element type of the audited collection, or its ID type if a many-to-many.
        /// </summary>
        Type AuditValueType { get; }

        /// <summary>
        /// Gets an auditable representation of a collection element.
        /// </summary>
        IRelationAuditValueResolver AuditValueResolver { get; }
    }
}
