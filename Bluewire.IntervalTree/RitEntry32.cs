namespace Bluewire.IntervalTree
{
    /// <summary>
    /// Encapsulates the three parts of a RIT entry, suitable for mapping as an immutable NHibernate component.
    /// </summary>
    public class RitEntry32
    {
        protected RitEntry32()
        {
        }

        public static RitEntry32 Missing => new RitEntry32 { Lower = int.MaxValue, Upper = int.MaxValue, Status = RitStatus.Missing };

        public RitEntry32(int lower, int upper, int? node, RitStatus status = RitStatus.Valid)
        {
            Lower = lower;
            Upper = upper;
            Node = node;
            Status = status;
        }

        public virtual int Lower { get; protected set; }
        public virtual int Upper { get; protected set; }
        public virtual int? Node { get; protected set; }
        public virtual RitStatus Status { get; protected set; }

        public override string ToString()
        {
            return $"{Lower}:{Node?.ToString() ?? "*"}:{Upper} ({Status})";
        }
    }
}
