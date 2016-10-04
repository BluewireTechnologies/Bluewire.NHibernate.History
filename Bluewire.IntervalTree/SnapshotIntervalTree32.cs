using System;

namespace Bluewire.IntervalTree
{
    /// <summary>
    /// Templates the generation of signed 32-bit RIT entries and query parameters, based on the implementation of
    /// MapIntervalBoundary which maps values of type T onto the set of positive integers between +/- 0x7fffffff,
    /// in such a way as to facilitate 'moment in time'-type queries.
    /// This requires slightly unusual handling of boundaries. RI-Trees are traditionally intended to find overlapping
    /// intervals at a certain resolution, which means that the minimum match interval is one unit wide. In order to
    /// emulate a 'zero width' snapshot interval, we need to tamper with the start and end boundaries of indexed entries.
    /// </summary>
    public abstract class SnapshotIntervalTree32<T>
    {
        private readonly RitCalculator32 treeDefinition;

        protected SnapshotIntervalTree32(RitCalculator32 treeDefinition)
        {
            this.treeDefinition = treeDefinition;
        }

        /// <summary>
        /// Convert the specified interval boundary value into a 32-bit number representing its position on the timeline, rounding
        /// downward if necessary.
        /// </summary>
        /// <param name="value">Value to map onto the 32-bit timeline.</param>
        /// <param name="isRoundedDown">True if the result is rounded down, false if it was precise.</param>
        protected abstract int MapIntervalBoundary(T value, out bool isRoundedDown);

        public RitEntry32 CalculateNode(T start, T end)
        {
            var lower = GetLowerBound(start);
            var upper = GetUpperBound(end);
            return GetNodeForBoundaries(lower, upper);
        }

        public RitEntry32 CalculateNodeWithoutEnd(T start)
        {
            var lower = GetLowerBound(start);
            return new RitEntry32(lower, Int32.MaxValue, treeDefinition.GetForkNode(lower, Int32.MaxValue));
        }

        public int GetUpperBound(T end)
        {
            bool isRoundedDown;
            var upper = MapIntervalBoundary(end, out isRoundedDown);
            if (!isRoundedDown) upper--; // Force the upper boundary downwards, unless it was already rounded down.
            return upper;
        }

        public int GetLowerBound(T start)
        {
            bool isRoundedDown;
            var lower = MapIntervalBoundary(start, out isRoundedDown);
            if (isRoundedDown) lower++; // Round the lower boundary up.
            return lower;
        }

        public RitQuery32 GenerateQuery(T start, T end)
        {
            bool isRoundedDown;
            var lower = MapIntervalBoundary(start, out isRoundedDown);
            var upper = MapIntervalBoundary(end, out isRoundedDown);
            if (lower > upper) throw new ArgumentException($"The value of '{nameof(end)}' must not map logically-earlier than the value of '{nameof(start)}'.");

            return treeDefinition.GenerateQuery(lower, upper);
        }

        private RitEntry32 GetNodeForBoundaries(int lower, int upper)
        {
            if (lower > upper) return new RitEntry32(lower, upper, null);
            return new RitEntry32(lower, upper, treeDefinition.GetForkNode(lower, upper));
        }
    }
}
