using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Bluewire.NHibernate.Audit.Runtime
{
    public class SessionAuditInfo
    {
        private readonly IAuditDatestampProvider datestampProvider;
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

        private DateTimeOffset? flushDatestamp;
        private int flushDepth;

        public SessionAuditInfo(IAuditDatestampProvider datestampProvider)
        {
            this.datestampProvider = datestampProvider;
        }

        public bool IsFlushing => flushDatestamp != null;

        public void AssertIsFlushing()
        {
            if (!IsFlushing) throw new InvalidOperationException("No flush in progress when one was expected.");
        }

        public DateTimeOffset OperationDatestamp
        {
            get
            {
                return flushDatestamp ?? NowAtRoundTrippablePrecision();
            }
        }

        private DateTimeOffset NowAtRoundTrippablePrecision()
        {
            // We cannot rely on all systems preserving the precision of a DateTimeOffset.
            // In particular, databases may truncate nanoseconds or microseconds. We can
            // reasonably demand millisecond accuracy, though this does mean that the
            // 'datetime' type in SQL Server cannot be used (only accurate to about 3ms).
            var now = datestampProvider.GetDatestampForNow();
            return new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond, now.Offset);
        }

        public void BeginFlush()
        {
            lock (this)
            {
                flushDepth++;
                if (flushDepth <= 1)
                {
                    Debug.Assert(flushDatestamp == null);
                    flushDatestamp = NowAtRoundTrippablePrecision();
                }
                else
                {
                    Debug.Assert(flushDatestamp != null);
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
                    Debug.Assert(flushDatestamp != null);
                    flushDatestamp = null;
                }
                else
                {
                    Debug.Assert(flushDatestamp != null);
                }
            }
        }

        public class EntityState
        {
        }
    }
}
