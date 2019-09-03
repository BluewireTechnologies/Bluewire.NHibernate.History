using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToOne
{
    public class ChildEntity
    {
        public virtual EntityWithOneToOne __Owner { get; set; }
        public virtual int Id { get; set; }
        public virtual int Value { get; set; }
    }

    [AuditableEntity(typeof(EntityWithOneToOneAuditHistory))]
    public class EntityWithOneToOne
    {
        private ChildEntity reference;
        public virtual int Id { get; set; }

        public virtual ChildEntity Reference
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

    public class EntityWithOneToOneAuditHistory : EntityAuditHistoryBase<int, int>
    {
        public virtual int? ReferenceId { get; set; }
        public virtual int? ReferenceValue { get; set; }
    }
}
