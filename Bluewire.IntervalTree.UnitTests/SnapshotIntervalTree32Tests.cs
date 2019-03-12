using System;
using System.Linq;
using NUnit.Framework;

namespace Bluewire.IntervalTree.UnitTests
{
    [TestFixture]
    public class SnapshotIntervalTree32Tests
    {
        private readonly DivideByFourSnapshotIntervalTree tree = new DivideByFourSnapshotIntervalTree(RitCalculator32.PositiveOnly);

        class DivideByFourSnapshotIntervalTree : SnapshotIntervalTree32<int>
        {
            public DivideByFourSnapshotIntervalTree(RitCalculator32 treeDefinition) : base(treeDefinition)
            {
            }

            protected override int MapIntervalBoundary(int value, out bool isRoundedDown)
            {
                isRoundedDown = value % 4 != 0;
                return Map(value);
            }

            private static int Map(int value)
            {
                return value / 4;
            }
        }

        class MacroInterval
        {
            public int Start { get; set; }
            public int End { get; set; }
            public RitEntry32 Rit { get; set; }
        }

        [Test]
        public void NodeWithoutEndHasMaximumUpperBound()
        {
            var fullTree = new DivideByFourSnapshotIntervalTree(RitCalculator32.MaximumRange);
            var entry = fullTree.CalculateNodeWithoutEnd(-42);
            Assert.That(entry.Upper, Is.EqualTo(Int32.MaxValue));
            Assert.That(entry.Node, Is.EqualTo(0)); // With a start below 0 and an end at infinity, only the root could be the fork node.
        }

        [Test]
        public void QueryingSnapshotReturnsExactlyOneCorrectMatch()
        {
            var set = new [] {
                CreateMacroInterval(10, 34),    // Fork = 8
                CreateMacroInterval(34, 36),    // N/A, never crosses a RIT boundary.
                CreateMacroInterval(36, 45)     // Fork = 10
            };

            // At most one match:
            for (var i = 0; i < 64; i++)
            {
                Assert.That(set.Where(CreateFilter(i)).Count(), Is.LessThanOrEqualTo(1));
            }

            // Verify specific cases:
            Assert.That(set.Where(CreateFilter(10)), Is.Empty); // 10 is rounded down to a RIT boundary which is before item 0 'exists'.
            Assert.That(set.Where(CreateFilter(32)).Single(), Is.EqualTo(set[0]));
            Assert.That(set.Where(CreateFilter(34)).Single(), Is.EqualTo(set[0]));
            Assert.That(set.Where(CreateFilter(36)).Single(), Is.EqualTo(set[2]));
        }

        private Func<MacroInterval, bool> CreateFilter(int snapshot)
        {
            var query = tree.GenerateQuery(snapshot, snapshot);
            var filter = query.ToFilter();
            return i => filter(i.Rit);
        }

        private MacroInterval CreateMacroInterval(int start, int end)
        {
            return new MacroInterval {
                Start = start,
                End = end,
                Rit = tree.CalculateNode(start, end)
            };
        }
    }
}
