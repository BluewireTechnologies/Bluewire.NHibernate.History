using System;

namespace Bluewire.NHibernate.Audit
{
    public interface IAuditHistory
    {
        /// <summary>
        /// Primary key of the audit table.
        /// </summary>
        long AuditId { get; }
        object VersionId { get; set; }
        object Id { get; }
        object PreviousVersionId { get; set; }
        DateTimeOffset AuditDatestamp { get; set; }
        AuditedOperation AuditedOperation { get; set; }
    }

    public abstract class SetRelationAuditHistoryEntry<TEntityKey, TValue> : ISetRelationAuditHistory
    {
        public virtual long AuditId { get; protected set; }
        public virtual DateTimeOffset StartDatestamp { get; protected set; }
        public virtual DateTimeOffset? EndDatestamp { get; protected set; }
        public virtual TEntityKey OwnerId { get; protected set; }
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

        object IRelationAuditHistory.Value
        {
            get { return Value; }
            set { Value = (TValue)value; }
        }
    }

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

    public interface IRelationAuditHistory
    {
        long AuditId { get; }
        DateTimeOffset StartDatestamp { get; set; }
        DateTimeOffset? EndDatestamp { get; set; }
        object OwnerId { get; set; }
        object Value { get; set; }
    }

    public interface IKeyedRelationAuditHistory : IRelationAuditHistory
    {
        object Key { get; set; }
    }

    public interface ISetRelationAuditHistory : IRelationAuditHistory
    {
    }
}