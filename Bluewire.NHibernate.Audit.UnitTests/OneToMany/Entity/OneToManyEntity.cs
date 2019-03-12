using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Entity
{
    [AuditableEntity(typeof(OneToManyEntityAuditHistory))]
    public class OneToManyEntity
    {
        public virtual int Id { get; set; }
        public virtual string Value { get; set; }
        public virtual int VersionId { get; set; }
    }

    public class OneToManyEntityAuditHistory : EntityAuditHistoryBase<int, int>
    {
        public virtual string Value { get; set; }
    }
}
