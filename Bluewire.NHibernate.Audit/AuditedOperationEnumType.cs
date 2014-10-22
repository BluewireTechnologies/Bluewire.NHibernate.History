using NHibernate.Type;

namespace Bluewire.NHibernate.Audit
{
    public class AuditedOperationEnumType : PersistentEnumType
    {
        public AuditedOperationEnumType() : base(typeof(AuditedOperation))
        {
        }
    }
}