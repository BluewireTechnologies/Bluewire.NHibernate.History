using System;
using Bluewire.NHibernate.Audit.Attributes;
using Iesi.Collections.Generic;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Entity
{
    [AuditableEntity(typeof(EntityWithSetOfEntityTypesAuditHistory))]
    public class EntityWithSetOfEntityTypes
    {
        public EntityWithSetOfEntityTypes()
        {
            Entities = new HashedSet<OneToManyEntity>();
        }

        public virtual int Id { get; set; }

        [AuditableRelation(typeof(EntityWithSetOfEntityTypesEntitiesAuditHistory))]
        public virtual ISet<OneToManyEntity> Entities { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithSetOfEntityTypesAuditHistory : IEntityAuditHistory
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
    
    public class EntityWithSetOfEntityTypesEntitiesAuditHistory : SetRelationAuditHistoryEntry<int, int>
    {
    }
}
