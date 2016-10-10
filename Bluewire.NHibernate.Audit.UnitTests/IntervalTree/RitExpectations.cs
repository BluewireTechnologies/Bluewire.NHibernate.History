using System;
using Bluewire.IntervalTree;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.IntervalTree
{
    public class RitExpectations
    {
        private readonly SnapshotIntervalTree32<DateTimeOffset> interval;

        public RitExpectations(SnapshotIntervalTree32<DateTimeOffset> interval)
        {
            this.interval = interval;
        }

        /// <summary>
        /// Verifies that a freshly-written RitEntry is correct.
        /// </summary>
        public void VerifyCurrentRitEntry(RitEntry32 current, DateTimeOffset datestamp)
        {
            Assert.That(current, Is.Not.Null);
            Assert.That(current.Status, Is.EqualTo(RitStatus.Valid));
            Assert.That(current, Is.EqualTo(interval.CalculateNodeWithoutEnd(datestamp)).Using(new RitEntry32EqualityComparer()));
        }

        /// <summary>
        /// Verifies that a freshly-invalidated RitEntry is correct:
        /// * Upper bound is correct
        /// * Node needs update
        /// </summary>
        public void VerifyPreviousRitEntry(RitEntry32 previous, DateTimeOffset datestamp)
        {
            Assert.That(previous, Is.Not.Null);
            Assert.That(previous.Status, Is.EqualTo(RitStatus.NodeNeedsUpdate));
            Assert.That(previous.Upper, Is.EqualTo(interval.GetUpperBound(datestamp)));
        }
    }
}
