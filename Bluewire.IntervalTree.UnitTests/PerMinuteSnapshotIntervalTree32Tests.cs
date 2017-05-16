using System;
using NUnit.Framework;

namespace Bluewire.IntervalTree.UnitTests
{
    [TestFixture]
    public class PerMinuteSnapshotIntervalTree32Tests
    {
        [Test]
        public void QueryingSnapshotImmediatelyBeforeStart_DoesNotIncludeEntry()
        {
            var tree = new PerMinuteSnapshotIntervalTree32();
            var datestamp = new DateTimeOffset(2017, 05, 09, 11, 37, 10, 277, TimeSpan.FromHours(1));
            var snapshot = new DateTime(2017, 05, 09, 11, 37, 0);   // Rounded down to the previous minute.
            var entry = tree.CalculateNodeWithoutEnd(datestamp);

            var query = tree.GenerateQuery(snapshot, snapshot);
            Assert.False(query.ToFilter()(entry));
        }

        [Test]
        public void QueryingSnapshotImmediatelyAfterStart_IncludesEntry()
        {
            var tree = new PerMinuteSnapshotIntervalTree32();
            var datestamp = new DateTimeOffset(2017, 05, 09, 11, 37, 10, 277, TimeSpan.FromHours(1));
            var snapshot = new DateTime(2017, 05, 09, 11, 38, 0);   // Rounded up to the next minute.
            var entry = tree.CalculateNodeWithoutEnd(datestamp);

            var query = tree.GenerateQuery(snapshot, snapshot);
            Assert.True(query.ToFilter()(entry));
        }

        [Test]
        public void QueryingSnapshotExactlyAtStart_IncludesEntry()
        {
            var tree = new PerMinuteSnapshotIntervalTree32();
            var datestamp = new DateTimeOffset(2017, 05, 09, 11, 37, 0, 0, TimeSpan.FromHours(1));
            var snapshot = new DateTime(2017, 05, 09, 11, 37, 0);
            var entry = tree.CalculateNodeWithoutEnd(datestamp);

            var query = tree.GenerateQuery(snapshot, snapshot);
            Assert.True(query.ToFilter()(entry));
        }

        [Test]
        public void QueryingSnapshotExactlyAtEnd_DoesNotIncludeEntry()
        {
            var tree = new PerMinuteSnapshotIntervalTree32();
            var datestamp = new DateTimeOffset(2017, 05, 09, 11, 37, 0, 0, TimeSpan.FromHours(1));
            var snapshot = new DateTime(2017, 05, 09, 11, 37, 0);
            var entry = tree.CalculateNode(new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeSpan.FromHours(1)), datestamp);

            var query = tree.GenerateQuery(snapshot, snapshot);
            Assert.False(query.ToFilter()(entry));
        }
    }
}
