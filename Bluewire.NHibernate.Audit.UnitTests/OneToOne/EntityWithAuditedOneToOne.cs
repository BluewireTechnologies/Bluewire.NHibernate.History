using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToOne
{
    public class AuditedChildEntity
    {
        public virtual EntityWithAuditedOneToOne __Owner { get; set; }
        public virtual int Id { get; set; }
        public virtual int Value { get; set; }
    }

    [AuditableEntity(typeof(EntityWithAuditedOneToOneAuditHistory))]
    public class EntityWithAuditedOneToOne
    {
        private AuditedChildEntity reference;
        public virtual int Id { get; set; }

        [AuditableRelation]
        public virtual AuditedChildEntity Reference
        {
            get => reference;
            set
            {
                if (reference != null)
                {
                    reference.__Owner = null;
                    reference.Id = default(int);
                }
                reference = value;
                if (reference != null) reference.__Owner = this;
            }
        }

        public virtual int VersionId { get; set; }
    }

    public class EntityWithAuditedOneToOneAuditHistory : EntityAuditHistoryBase<int, int>
    {
        public virtual int? ReferenceId { get; set; }
        public virtual int? ReferenceValue { get; set; }
    }
}
