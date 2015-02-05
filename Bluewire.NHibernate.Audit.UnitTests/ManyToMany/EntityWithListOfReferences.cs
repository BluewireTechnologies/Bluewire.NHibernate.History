﻿using System;
using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.ManyToMany
{
    [AuditableEntity(typeof(EntityWithListOfReferencesAuditHistory))]
    public class EntityWithListOfReferences
    {
        public EntityWithListOfReferences()
        {
            Entities = new List<ReferencableEntity>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithListOfReferencesEntitiesAuditHistory))]
        public virtual IList<ReferencableEntity> Entities { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithListOfReferencesAuditHistory : IEntityAuditHistory
    {
        public virtual int Id { get; set; }
        public virtual int? VersionId { get; set; }

        public virtual long AuditId { get; protected set; }
        public virtual int? PreviousVersionId { get; protected set; }

        object IEntityAuditHistory.VersionId
        {
            get { return VersionId; }
            set { VersionId = (int?)value; }
        }

        object IEntityAuditHistory.Id
        {
            get { return Id; }
        }

        object IEntityAuditHistory.PreviousVersionId
        {
            get { return PreviousVersionId; }
            set { PreviousVersionId = (int?)value; }
        }

        public virtual DateTimeOffset AuditDatestamp { get; set; }
        public virtual AuditedOperation AuditedOperation { get; set; }
    }

    public class EntityWithListOfReferencesEntitiesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, int>
    {
    }
}
