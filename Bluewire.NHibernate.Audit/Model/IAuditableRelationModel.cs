﻿using System;

namespace Bluewire.NHibernate.Audit.Model
{
    public interface IAuditableRelationModel
    {
        string CollectionRole { get; }
        /// <summary>
        /// The entity type recorded for each change. Must derive from SetRelationAuditHistoryEntry&lt;,&gt; or KeyedRelationAuditHistoryEntry&lt;,,&gt;.
        /// </summary>
        Type AuditEntryType { get; }
        /// <summary>
        /// The type of value stored against each audited change. Usually the same as the element type of the audited collection, or its ID type if a many-to-many.
        /// </summary>
        Type AuditValueType { get; }

        /// <summary>
        /// Gets an auditable representation of a collection element.
        /// </summary>
        IRelationAuditValueResolver AuditValueResolver { get; }
    }
}