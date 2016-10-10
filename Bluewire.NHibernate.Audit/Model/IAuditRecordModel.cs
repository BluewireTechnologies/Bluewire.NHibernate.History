using System;

namespace Bluewire.NHibernate.Audit.Model
{
    public interface IAuditRecordModel
    {
        /// <summary>
        /// The entity type recorded for each change. Must derive from:
        /// * IEntityAuditHistory for entity history records,
        /// * SetRelationAuditHistoryEntry&lt;,&gt; for non-indexed (set-like) collection history records,
        /// * KeyedRelationAuditHistoryEntry&lt;,,&gt; for indexed (list or map) collection history records.
        /// </summary>
        Type AuditEntryType { get; }

        /// <summary>
        /// Optional RI-Tree property, for snapshot queries.
        /// </summary>
        RitSnapshotPropertyModel32 RitProperty { get; }
    }
}
