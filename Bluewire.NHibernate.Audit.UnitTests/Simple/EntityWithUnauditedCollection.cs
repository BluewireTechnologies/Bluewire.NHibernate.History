using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bluewire.NHibernate.Audit.Attributes;
using Bluewire.NHibernate.Audit.UnitTests.ManyToMany;

namespace Bluewire.NHibernate.Audit.UnitTests.Simple
{
    [AuditableEntity(typeof(EntityWithUnauditedCollectionAuditHistory))]
    public class EntityWithUnauditedCollection
    {
        public EntityWithUnauditedCollection()
        {
            Entities = new List<ReferencableEntity>();
        }

        public virtual int Id { get; set; }
        public virtual string Value { get; set; }
        public virtual int VersionId { get; set; }

        public virtual IList<ReferencableEntity> Entities { get; protected set; }
    }

    public class EntityWithUnauditedCollectionAuditHistory : IAuditHistory
    {
        public virtual int Id { get; protected set; }
        public virtual string Value { get; set; }
        public virtual int? VersionId { get; protected set; }

        public virtual long AuditId { get; protected set; }
        public virtual int? PreviousVersionId { get; protected set; }

        object IAuditHistory.VersionId
        {
            get { return VersionId; }
            set { VersionId = (int?)value; }
        }

        object IAuditHistory.Id
        {
            get { return Id; }
        }

        object IAuditHistory.PreviousVersionId
        {
            get { return PreviousVersionId; }
            set { PreviousVersionId = (int?)value; }
        }

        public virtual DateTimeOffset AuditDatestamp { get; set; }
        public virtual AuditedOperation AuditedOperation { get; set; }
    }
}
