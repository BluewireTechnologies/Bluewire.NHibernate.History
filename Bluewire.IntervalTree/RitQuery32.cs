using System.Collections.Generic;

namespace Bluewire.IntervalTree
{
    /// <summary>
    /// The query identifies three sets (left, middle and right respectively):
    /// * from i where i.Node in :LeftNodes and i.Upper &gt;= :Lower
    /// * from i where i.Node &gt;= :Lower and i.Node &lt;= :Upper
    /// * from i where i.Node in :RightNodes and i.Lower &lt;= :Upper
    /// The union of these is equivalent to the pseudo-SQL:
    /// * from i where (i.Lower to i.Upper) overlaps (:Lower to :Upper)
    /// </summary>
    public class RitQuery32
    {
        public int Lower { get; set; }
        public int Upper { get; set; }
        public IList<int> LeftNodes { get; set; }
        public IList<int> RightNodes { get; set; }
    }
}
