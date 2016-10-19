using System.Linq;
using NUnit.Framework;

namespace Bluewire.IntervalTree.UnitTests
{
    /// <summary>
    /// Sanity checks, based on http://sqlmag.com/t-sql/sql-server-interval-queries .
    /// </summary>
    [TestFixture]
    public class RitCalculator32SanityChecks
    {
        [TestCase(11, 13, 12)]
        public void CalculatesCorrectForkNode(int lower, int upper, int fork)
        {
            Assert.That(RitCalculator32.PositiveOnly.GetForkNode(lower, upper), Is.EqualTo(fork));
        }

        [TestCase(11, new [] { 8, 10 })]
        public void CalculatesCorrectLeftNodes(int lower, int[] leftNodes)
        {
            Assert.That(RitCalculator32.PositiveOnly.GetLeftNodes(lower), Is.EqualTo(leftNodes));
        }

        // Values corresponding to the first 19 right nodes for all these test cases.
        private readonly int[] TopFiveBits = new [] {
            0x40000000, 0x20000000, 0x10000000,
            0x08000000, 0x04000000, 0x02000000, 0x01000000,
            0x00800000, 0x00400000, 0x00200000, 0x00100000,
            0x00080000, 0x00040000, 0x00020000, 0x00010000,
            0x00008000, 0x00004000, 0x00002000, 0x00001000
        };

        [TestCase(13, new [] { 0x0800, 0x0400, 0x0200, 0x0100, 0x0080, 0x0040, 0x0020, 16, 14 })]
        public void CalculatesCorrectRightNodes(int upper, int[] rightNodes)
        {
            Assert.That(RitCalculator32.PositiveOnly.GetRightNodes(upper), Is.EqualTo(TopFiveBits.Concat(rightNodes)));
        }
    }
}
