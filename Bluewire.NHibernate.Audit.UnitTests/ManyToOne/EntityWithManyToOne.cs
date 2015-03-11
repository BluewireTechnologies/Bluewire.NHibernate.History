using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.ManyToOne
{
    public class UnauditedEntity
    {
        public virtual int Id { get; set; }
    }

    [AuditableEntity(typeof(EntityWithManyToOneAuditHistory))]
    public class EntityWithManyToOne
    {
        public virtual int Id { get; set; }
        public virtual UnauditedEntity Reference { get; set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithManyToOneAuditHistory : EntityAuditHistoryBase<int, int>
    {
        public virtual int? ReferenceId { get; set; }
    }
}
