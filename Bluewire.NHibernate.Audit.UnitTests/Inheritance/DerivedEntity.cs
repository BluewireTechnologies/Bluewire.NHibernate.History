using Bluewire.NHibernate.Audit.Attributes;

namespace Bluewire.NHibernate.Audit.UnitTests.Inheritance
{
    //[AuditableEntity(typeof(BaseEntityAuditHistory))]
    public abstract class BaseEntity
    {
        public virtual int Id { get; set; }
        public virtual int VersionId { get; set; }
    }

    [AuditableEntity(typeof(DerivedEntityAuditHistory))]
    public class DerivedEntity : BaseEntity
    {
        public virtual string Value { get; set; }
    }

    public class BaseEntityAuditHistory : EntityAuditHistoryBase<int, int>
    {
    }

    public class DerivedEntityAuditHistory : BaseEntityAuditHistory
    {
        public virtual string Value { get; set; }
    }
}
