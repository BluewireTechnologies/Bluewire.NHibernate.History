using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.Simple
{
    [AuditableEntity(typeof(SimpleEntityAuditHistory))]
    public class SimpleEntity
    {
        public virtual int Id { get; set; }
        public virtual string Value { get; set; }
        public virtual int VersionId { get; set; }
    }

    public class SimpleEntityAuditHistory : EntityAuditHistoryBase<int, int>
    {
        public virtual string Value { get; set; }
    }
}
