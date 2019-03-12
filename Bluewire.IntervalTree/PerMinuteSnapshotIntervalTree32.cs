using System;
using System.Diagnostics;

namespace Bluewire.IntervalTree
{
    /// <summary>
    /// Maps a DateTimeOffset to a 32-bit integer range, at one-minute resolution.
    /// Each boundary between minutes in UTC corresponds to one integer value.
    /// There will often be gaps between allocated integer values.
    /// </summary>
    public class PerMinuteSnapshotIntervalTree32 : SnapshotIntervalTree32<DateTimeOffset>
    {
        private readonly int epochYear;

        /// <summary>
        /// Create a per-minute snapshot interval based on an epoch of the year 2000.
        /// </summary>
        public PerMinuteSnapshotIntervalTree32() : this(2000)
        {
        }

        public PerMinuteSnapshotIntervalTree32(int epochYear) : base(RitCalculator32.MaximumRange)
        {
            this.epochYear = epochYear;
        }

        protected override int MapIntervalBoundary(DateTimeOffset value, out bool isRoundedDown)
        {
            var yearsBeyondTheEpoch = value.Year - epochYear;

            // Bit assignments are:
            // yyyyyyyyyyyy ddddddddd hhhhh mmmmmm

            // This can therefore endure:
            // * up to 64 minutes in an hour (6 bits)
            Debug.Assert(value.Minute >> 6 == 0);
            // * up to 32 hours in a day (5 bits)
            Debug.Assert(value.Hour >> 5 == 0);
            // * up to 512 days in a year (9 bits)
            Debug.Assert(value.DayOfYear >> 9 == 0);
            // * until 4096 years beyond the epoch (12 bits)
            Debug.Assert(yearsBeyondTheEpoch > 0);
            Debug.Assert(yearsBeyondTheEpoch >> 12 == 0);
            // * at one-minute resolution.
            // Should any of these assumptions be violated, a new implementation will be required
            // and all interval boundaries ever calculated will need to be regenerated.

            var lowBits = (((
                value.DayOfYear
                << 5) + value.Hour)
                           << 6) + value.Minute;
            Debug.Assert(lowBits > 0);

            // 0 <= yearsBeyondTheEpoch < 4096
            // but we want:
            // -2048 <= yearsBeyondTheEpoch < 2048
            var yearsRecentred = yearsBeyondTheEpoch - 0x800;
            // and then we want it shifted into the top 12 bits.
            var yearsShifted = yearsRecentred << 20;

            if (value.Second != 0)
            {
                isRoundedDown = true;
            }
            else if (value.Millisecond != 0)
            {
                isRoundedDown = true;
            }
            else
            {
                // Slow path, within the very millisecond of a minute boundary.
                isRoundedDown = value.TimeOfDay - new TimeSpan(value.Hour, value.Minute, 0) != TimeSpan.Zero;
            }
            Debug.Assert((lowBits & yearsShifted) == 0);
            return lowBits | yearsShifted;
        }
    }
}
