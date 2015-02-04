using System;
using System.Diagnostics;
using System.Threading;
using Bluewire.Common.Time;
using NHibernate;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Runtime
{
    public class SessionAuditInfo : ISessionSnapshot
    {
        private readonly IClock clock;
        
        private readonly ThreadLocal<DateTimeOffset?> flushDatestamp = new ThreadLocal<DateTimeOffset?>();
        private int flushDepth;
        private bool isFinalisingFlush;
        private DeferredAuditModel deferredModel;

        public DeferredAuditModel CurrentModel
        {
            get
            {
                return deferredModel;
            }
        }

        IDeferredAudit ISessionSnapshot.CurrentModel
        {
            get
            {
                return deferredModel;
            }
        }

        public SessionAuditInfo(IClock clock)
        {
            this.clock = clock;
            deferredModel = new DeferredAuditModel();
        }

        public void AssertIsFlushing()
        {
            var v = flushDatestamp.Value;
            if (v == null) throw new InvalidOperationException("No flush in progress when one was expected.");
            Debug.Assert(deferredModel != null);
        }

        public DateTimeOffset OperationDatestamp
        {
            get
            {
                return flushDatestamp.Value ?? clock.Now;
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
                    flushDatestamp.Value = clock.Now;
                }
                else
                {
                    Debug.Assert(flushDatestamp.Value != null);
                }
            }
        }

        public void EndFlush(IEventSource session)
        {
            lock (this)
            {
                Debug.Assert(flushDepth > 0);
                if (flushDepth == 1 && !isFinalisingFlush)
                {
                    Debug.Assert(flushDatestamp.Value != null);
                    isFinalisingFlush = true;
                }
                else
                {
                    Debug.Assert(flushDatestamp.Value != null);
                    flushDepth--;
                    return;
                }
            }
            try
            {
                FlushAuditTasks(session);
            }
            catch
            {
                // Any error during flush is utterly unrecoverable. Destroy the session and force the transaction to roll back.
                if(session.Transaction.IsActive) session.Transaction.Rollback();
                session.Clear();
                session.Close();
                throw;
            }
            finally
            {
                lock (this)
                {
                    Debug.Assert(isFinalisingFlush);
                    Debug.Assert(flushDatestamp.Value != null);
                    flushDepth--;
                    flushDatestamp.Value = null;
                    isFinalisingFlush = false;
                    deferredModel = new DeferredAuditModel();
                }
            }
        }

        private void FlushAuditTasks(IEventSource session)
        {
            var inner = session.GetSession(EntityMode.Poco);
            while (deferredModel.PendingFixups.Count > 0)
            {
                var fixup = deferredModel.PendingFixups.Dequeue();
                fixup.Apply(inner.GetSessionImplementation(), this);
            }
            while (deferredModel.PendingWorkItems.Count > 0)
            {
                var work = deferredModel.PendingWorkItems.Dequeue();
                work.Execute(inner.GetSessionImplementation(), this);
            }
            while (deferredModel.PendingInserts.Count > 0)
            {
                var item = deferredModel.PendingInserts.Dequeue();
                inner.Save(item);
            }
            inner.Flush();
        }

    }
}