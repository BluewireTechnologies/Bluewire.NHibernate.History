﻿using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.ManyToMany
{
    [AuditableEntity(typeof(EntityWithSetOfReferencesAuditHistory))]
    public class EntityWithSetOfReferences
    {
        public EntityWithSetOfReferences()
        {
            Entities = new HashSet<ReferencableEntity>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithSetOfReferencesEntitiesAuditHistory))]
        public virtual ICollection<ReferencableEntity> Entities { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithSetOfReferencesAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class EntityWithSetOfReferencesEntitiesAuditHistory : SetRelationAuditHistoryEntry<int, int>
    {
    }
}
