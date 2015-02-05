using System;
using Bluewire.NHibernate.Audit.Meta;

namespace Bluewire.NHibernate.Audit
{
    /// <summary>
    /// Base class for auditing keyed collections.
    /// </summary>
    /// <typeparam name="TEntityKey"></typeparam>
    /// <typeparam name="TCollectionKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public abstract class KeyedRelationAuditHistoryEntry<TEntityKey, TCollectionKey, TValue> : IKeyedRelationAuditHistory
    {
        public virtual long AuditId { get; protected set; }
        public virtual DateTimeOffset StartDatestamp { get; protected set; }
        public virtual DateTimeOffset? EndDatestamp { get; protected set; }
        public virtual TEntityKey OwnerId { get; protected set; }
        public virtual TCollectionKey Key { get; protected set; }
        public virtual TValue Value { get; protected set; }

        DateTimeOffset IRelationAuditHistory.StartDatestamp
        {
            get { return StartDatestamp; }
            set { StartDatestamp = value; }
        }

        DateTimeOffset? IRelationAuditHistory.EndDatestamp
        {
            get { return EndDatestamp; }
            set { EndDatestamp = value; }
        }

        object IRelationAuditHistory.OwnerId
        {
            get { return OwnerId; }
            set { OwnerId = (TEntityKey)value; }
        }
        object IKeyedRelationAuditHistory.Key
        {
            get { return Key; }
            set { Key = (TCollectionKey)value; }
        }

        object IRelationAuditHistory.Value
        {
            get { return Value; }
            set { Value = (TValue)value; }
        }
    }
}