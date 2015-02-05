using System;
using System.Collections.Generic;
using Bluewire.NHibernate.Audit.Attributes;
using Bluewire.NHibernate.Audit.Meta;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Entity
{
    [AuditableEntity(typeof(EntityWithListOfEntityTypesAuditHistory))]
    public class EntityWithListOfEntityTypes
    {
        public EntityWithListOfEntityTypes()
        {
            Entities = new List<OneToManyEntity>();
        }

        public virtual int Id { get; set; }

        [AuditableRelation(typeof(EntityWithListOfEntityTypesEntitiesAuditHistory))]
        public virtual IList<OneToManyEntity> Entities { get; protected set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithListOfEntityTypesAuditHistory : IEntityAuditHistory
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

    public class EntityWithListOfEntityTypesEntitiesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, int>
    {
    }
}
