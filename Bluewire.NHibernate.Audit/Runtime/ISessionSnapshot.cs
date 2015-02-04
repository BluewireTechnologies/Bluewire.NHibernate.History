using System;

namespace Bluewire.NHibernate.Audit.Runtime
{
    /// <summary>
    /// Exposes information about the current audited flush operation.
    /// </summary>
    public interface ISessionSnapshot
    {
        IDeferredAudit CurrentModel { get; }
        DateTimeOffset OperationDatestamp { get; }
        void AssertIsFlushing();
    }
}