using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Meta;

namespace Bluewire.NHibernate.Audit.UnitTests.Query
{
    /// <summary>
    /// Helper for building an 'audit history' for testing queries.
    /// </summary>
    class MockAuditHistory
    {
        private readonly MockClock clock;
        private readonly List<IAuditRecord> history;

        public MockAuditHistory()
        {
            clock = new MockClock();
            history = new List<IAuditRecord>();
        }

        public void AdvanceTime()
        {
            clock.Advance(TimeSpan.FromSeconds(1));
        }

        public DateTimeOffset GetNow()
        {
            return clock.Now;
        }

        public void Audit<T>(T record) where T : EntityAuditHistoryBase<int, Guid>
        {
            record.VersionId = Guid.NewGuid();
            record.PreviousVersionId = history.OfType<T>().Where(h => Equals(h.Id, record.Id)).Select(h => h.VersionId).LastOrDefault();
            record.AuditDatestamp = clock.Now;
            history.Add(record);
        }

        public void AuditAddWithKey<T, TKey, TValue>(T record) where T : KeyedRelationAuditHistoryEntry<int, TKey, TValue>
        {
            IRelationAuditHistory r = record;
            r.StartDatestamp = clock.Now;
            history.Add(record);
        }

        public void AuditAddWithIndex<T, TValue>(T record) where T : KeyedRelationAuditHistoryEntry<int, int, TValue>
        {
            AuditAddWithKey<T, int, TValue>(record);
        }

        public void AuditRemoveWithKey<T, TKey, TValue>(T record) where T : KeyedRelationAuditHistoryEntry<int, TKey, TValue>
        {
            IRelationAuditHistory r = history.OfType<T>().Where(h => Equals(h.OwnerId, record.OwnerId) && Equals(h.Key, record.Key) && Equals(h.Value, record.Value)).Last();
            r.EndDatestamp = clock.Now;
        }

        public void AuditRemoveWithIndex<T, TValue>(T record) where T : KeyedRelationAuditHistoryEntry<int, int, TValue>
        {
            AuditRemoveWithKey<T, int, TValue>(record);
        }

        public void AuditAddToSet<T, TValue>(T record) where T : SetRelationAuditHistoryEntry<int, TValue>
        {
            IRelationAuditHistory r = record;
            r.StartDatestamp = clock.Now;
            history.Add(record);
        }

        public void AuditRemoveFromSet<T, TValue>(T record) where T : SetRelationAuditHistoryEntry<int, TValue>
        {
            IRelationAuditHistory r = history.OfType<T>().Where(h => Equals(h.OwnerId, record.OwnerId) && Equals(h.Value, record.Value)).Last();
            r.EndDatestamp = clock.Now;
        }

        public MockSnapshotContext At(DateTimeOffset snapshotDatestamp)
        {
            return new MockSnapshotContext(snapshotDatestamp, history.ToArray());
        }
    }
}
