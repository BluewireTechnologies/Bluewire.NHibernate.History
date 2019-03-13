using System;
using Bluewire.NHibernate.Audit.Meta;

namespace Bluewire.NHibernate.Audit
{
    /// <summary>
    /// Base implementation of the IEntityAuditHistory interface.
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TVersion"></typeparam>
    public abstract class EntityAuditHistoryBase<TId, TVersion> : IEntityAuditHistory<TId>
        where TVersion : struct
    {
        public virtual TId Id { get; set; }
        public virtual TVersion? VersionId { get; set; }
        public virtual TVersion? PreviousVersionId { get; set; }

        public virtual long AuditId { get; protected set; }
        public virtual DateTimeOffset AuditDatestamp { get; set; }
        public virtual AuditedOperation AuditedOperation { get; set; }

        object IEntityAuditHistory.VersionId
        {
            get { return VersionId; }
            set { VersionId = (TVersion?)value; }
        }

        object IEntityAuditHistory.Id
        {
            get { return Id; }
        }

        object IEntityAuditHistory.PreviousVersionId
        {
            get { return PreviousVersionId; }
            set { PreviousVersionId = (TVersion?)value; }
        }
    }
}
