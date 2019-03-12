using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bluewire.NHibernate.Audit
{
    public interface IAuditInfo
    {
        /// <summary>
        /// Get a snapshot datestamp consistent with current session state.
        /// </summary>
        /// <remarks>
        /// This method unavoidably forces a session flush.
        /// Subject to caveats below, this method endeavours to guarantee that queries against the returned
        /// snapshot will reflect all pending changes in the session. There is no need to perform a separate
        /// flush.
        /// Note that this refers to pending changes only. There is NO guarantee of the converse, that later
        /// changes will not appear in the snapshot.
        ///
        /// Guarantees offered by this method cannot be stronger than those offered by the clock. If the
        /// datestamp provider steps backwards through time, strange things may happen.
        /// </remarks>
        /// <param name="session"></param>
        /// <returns></returns>
        DateTimeOffset CommitSnapshot(ISession session);

        IAdvancedAuditInfo Advanced { get; }
    }
}
