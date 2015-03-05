using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Bluewire.Common.Time;

namespace Bluewire.NHibernate.Audit.Runtime
{
    public class SessionAuditInfo
    {
        private readonly IClock clock;
        private readonly Dictionary<object, EntityState> entityStates = new Dictionary<object, EntityState>();

        public EntityState GetState(object entity)
        {
            EntityState state;
            if (!entityStates.TryGetValue(entity, out state))
            {
                state = new EntityState();
                entityStates.Add(entity, state);
            }
            return state;
        }

        private readonly ThreadLocal<DateTimeOffset?> flushDatestamp = new ThreadLocal<DateTimeOffset?>();
        private int flushDepth;

        public SessionAuditInfo(IClock clock)
        {
            this.clock = clock;
        }

        public void AssertIsFlushing()
        {
            var v = flushDatestamp.Value;
            if (v == null) throw new InvalidOperationException("No flush in progress when one was expected.");
        }

        public DateTimeOffset OperationDatestamp
        {
            get
            {

                return flushDatestamp.Value ?? NowAtRoundTrippablePrecision();
            }
        }

        private DateTimeOffset NowAtRoundTrippablePrecision()
        {
            // We cannot rely on all systems preserving the precision of a DateTimeOffset.
            // In particular, databases may truncate nanoseconds or microseconds. We can
            // reasonably demand millisecond accuracy, though this does mean that the
            // 'datetime' type in SQL Server cannot be used (only accurate to about 3ms).
            var now = clock.Now;
            return new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond, now.Offset);
        }

        public void BeginFlush()
        {
            lock (this)
            {
                flushDepth++;
                if (flushDepth <= 1)
                {
                    Debug.Assert(flushDatestamp.Value == null);
                    flushDatestamp.Value = NowAtRoundTrippablePrecision();
                }
                else
                {
                    Debug.Assert(flushDatestamp.Value != null);
                }
            }
        }

        public void EndFlush()
        {
            lock (this)
            {
                Debug.Assert(flushDepth > 0);
                flushDepth--;
                if (flushDepth == 0)
                {
                    Debug.Assert(flushDatestamp.Value != null);
                    flushDatestamp.Value = null;
                }
                else
                {
                    Debug.Assert(flushDatestamp.Value != null);
                }
            }
        }

        public class EntityState
        {
        }
    }
}