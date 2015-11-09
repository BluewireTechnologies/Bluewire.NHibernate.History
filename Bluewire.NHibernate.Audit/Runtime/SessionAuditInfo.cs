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

        /// <summary>
        /// WARNING: Strictly, NHibernate defines its sessions to be 'not threadsafe' which is not the
        /// same thing as 'thread-locked'. Passing a session between threads is perfectly safe as long
        /// as only a single thread attempts to use it at a time. This makes it possible to make use of
        /// .NET 4.5's async capabilities without problem.
        /// The following member is ThreadLocal, however, which means that it is NOT safe for async use!
        /// Should probably be AsyncLocal when we update this library's framework version.
        /// Don't use LogicalCallContext for this prior to .NET 4.5! It does not flow correctly for async.
        /// </summary>
        private readonly ThreadLocal<DateTimeOffset?> flushDatestamp = new ThreadLocal<DateTimeOffset?>();
        private int flushDepth;

        public SessionAuditInfo(IAuditDatestampProvider datestampProvider)
        {
            this.datestampProvider = datestampProvider;
        }

        public bool IsFlushing { get { return flushDatestamp.Value != null; } }

        public void AssertIsFlushing()
        {
            if (!IsFlushing) throw new InvalidOperationException("No flush in progress when one was expected.");
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