using System;
using NUnit.Framework;

namespace Bluewire.IntervalTree.UnitTests
{
    /// <summary>
    /// Sanity checks, based on http://sqlmag.com/t-sql/sql-server-interval-queries .
    /// </summary>
    [TestFixture]
    public class RitCalculator32Tests
    {
        [Test]
        public void TreeRootedAtZeroCanUseEntire32BitRange()
        {
            Assert.That(new RitCalculator32(0).HalfRange, Is.EqualTo(Int32.MaxValue));
        }

        [Test]
        public void TreeRootedBelowZeroCannotUseEntirePositiveRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RitCalculator32(-1, Int32.MaxValue));
        }

        [Test]
        public void TreeRootedAtZeroCannotUseEntireNegativeRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RitCalculator32(0, Int32.MinValue));
        }

        [Test]
        public void TreeRootedAtMinusOneCanUseEntireNegativeRange()
        {
            new RitCalculator32(-1, Int32.MinValue);
        }

        [TestCase(0, 0x7fffffff, 0x40000000)]
        [TestCase(-1, 0x7fffffff, 0x40000000)]
        [TestCase(0x40000000, 0x3fffffff, 0x20000000)]
        public void CalculatesAppropriateHalfRangeAndStep(int root, int expectedHalfRange, int expectedStep)
        {
            var tree = new RitCalculator32(root);
            Assert.That(tree.HalfRange, Is.EqualTo(expectedHalfRange));
            Assert.That(tree.InitialStep, Is.EqualTo(expectedStep));
        }

        [Test]
        public void FuzzTest_ForkNodeIsAlwaysBetweenUpperAndLower()
        {
            var calculator = RitCalculator32.MaximumRange;
            var random = new Random();
            for (var i = 0; i < 1000000; i++)
            {
                var l = random.Next(Int32.MinValue, Int32.MaxValue);
                var u = random.Next(Int32.MinValue, Int32.MaxValue);
                if (l > u)
                {
                    var t = l;
                    l = u;
                    u = t;
                }
                var fork = calculator.GetForkNode(l, u);
                if (fork < l || fork > u) Assert.Fail($"Fork {fork} is not between {l} and {u}");
            }
        }

        // Cases taken from fuzz-test failures during development:
        [TestCase(748524609, 1983548486, 1073741824)]
        [TestCase(-1315064777, -1315064777, -1315064777)]
        public void CalculatesCorrectForkNode(int lower, int upper, int fork)
        {
            Assert.That(RitCalculator32.MaximumRange.GetForkNode(lower, upper), Is.EqualTo(fork));
        }
    }
}
