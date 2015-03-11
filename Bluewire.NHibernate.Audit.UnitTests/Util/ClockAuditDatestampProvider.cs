using System;
using Bluewire.Common.Time;

namespace Bluewire.NHibernate.Audit.UnitTests.Util
{
    /// <summary>
    /// Test implementation of IAuditDatestampProvider. Makes use of Bluewire.Common.Time.
    /// </summary>
    public class ClockAuditDatestampProvider : IAuditDatestampProvider
    {
        private readonly IClock clock;

        public ClockAuditDatestampProvider(IClock clock)
        {
            this.clock = clock;
        }

        public DateTimeOffset GetDatestampForNow()
        {
            return clock.Now;
        }
    }
}
