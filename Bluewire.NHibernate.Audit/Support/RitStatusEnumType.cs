using Bluewire.IntervalTree;
using NHibernate.Type;

namespace Bluewire.NHibernate.Audit.Support
{
    public class RitStatusEnumType : PersistentEnumType
    {
        public RitStatusEnumType() : base(typeof(RitStatus))
        {
        }
    }
}
