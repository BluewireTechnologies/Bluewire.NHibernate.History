using System;
using Bluewire.NHibernate.Audit.Model;

namespace Bluewire.NHibernate.Audit
{
    public interface IAdvancedAuditInfo
    {
        /// <summary>
        /// Get all types of history record known to the system.
        /// </summary>
        /// <returns></returns>
        Type[] GetKnownRecordTypes();

        /// <summary>
        /// Get the model definition associated with the specified record type, or null if it doesn't exist.
        /// </summary>
        /// <param name="recordType"></param>
        /// <returns></returns>
        IAuditRecordModel FindRecordModel(Type recordType);
    }
}
