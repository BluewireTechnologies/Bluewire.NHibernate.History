using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Bluewire.NHibernate.Audit.Support;
using NHibernate.Engine;

namespace Bluewire.NHibernate.Audit
{
    public class SessionAuditInfo
    {
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

        public DateTimeOffset FlushDatestamp
        {
            get
            {
                var v = flushDatestamp.Value;
                Debug.Assert(v != null);
                return v.Value;
            }
        }

        public void BeginFlush()
        {
            lock (this)
            {
                flushDepth++;
                if (flushDepth <= 1)
                {
                    Debug.Assert(flushDatestamp.Value == null);
                    flushDatestamp.Value = DateTimeOffset.Now;
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

        private static readonly WeakDictionary<ISessionImplementor, SessionAuditInfo> sessionInfos = new WeakDictionary<ISessionImplementor, SessionAuditInfo>();

        public static SessionAuditInfo For(ISessionImplementor session)
        {
            return sessionInfos.GetOrAdd(session, () => new SessionAuditInfo());
        }

        public class EntityState
        {
            public object PreviousVersionId { get; set; }
        }
    }
}