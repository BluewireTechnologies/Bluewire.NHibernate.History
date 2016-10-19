using System;

namespace Bluewire.IntervalTree
{
    /// <summary>
    /// Describes the state and validity of a persisted RitEntry32.
    /// </summary>
    [Flags]
    public enum RitStatus
    {
        Valid = 0,
        NodeNeedsUpdate = 0x0001,
        BoundsNeedUpdate = 0x0002,
        Invalid = 0x0003,
        Missing = 0xffff
    }
}
