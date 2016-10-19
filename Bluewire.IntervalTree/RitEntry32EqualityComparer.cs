using System.Collections.Generic;

namespace Bluewire.IntervalTree
{
    public sealed class RitEntry32EqualityComparer : IEqualityComparer<RitEntry32>
    {
        public bool Equals(RitEntry32 x, RitEntry32 y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Lower == y.Lower && x.Upper == y.Upper && x.Node == y.Node && x.Status == y.Status;
        }

        public int GetHashCode(RitEntry32 obj)
        {
            unchecked
            {
                var hashCode = obj.Lower;
                hashCode = (hashCode*397) ^ obj.Upper;
                hashCode = (hashCode*397) ^ obj.Node.GetHashCode();
                hashCode = (hashCode*397) ^ (int) obj.Status;
                return hashCode;
            }
        }
    }
}
