using System;
using System.Collections.Generic;

namespace Bluewire.IntervalTree
{
    public class RitCalculator32
    {
        public int Root { get; }
        public int HalfRange { get; }
        public int InitialStep { get; }

        /// <summary>
        /// Define a RI-Tree with the specified root node, using as much of the 32-bit range as possible.
        /// </summary>
        public RitCalculator32(int root) : this(root, root < 0 ? Int32.MinValue : Int32.MaxValue)
        {
        }

        /// <summary>
        /// Define a RI-Tree with the specified root node and upper or lower limit.
        /// </summary>
        /// <remarks>
        /// This is mainly a convenience for the other constructor, since it's easier to express the limit
        /// with the same sign as the root.
        /// </remarks>
        public RitCalculator32(int root, int limit)
        {
            this.Root = root;

            var upperLimitBound = Int32.MaxValue + 2 * Math.Min(root, 0);
            var lowerLimitBound = Int32.MinValue + 2 * Math.Max(root + 1, 0);

            if (limit > upperLimitBound || limit < lowerLimitBound)
            {
                throw new ArgumentOutOfRangeException($"A 32-bit RI-Tree rooted at 0x{root:X} cannot have a limit above 0x{upperLimitBound:X} or below 0x{lowerLimitBound:X}");
            }
            var halfRange = Math.Abs(limit - root);
            HalfRange = halfRange;
            InitialStep = halfRange / 2 + halfRange % 2; // Round up, carefully avoiding overflow.
        }
        
        /// <summary>
        /// A 31-bit RI-Tree occupying only the positive part of the 32-bit space.
        /// </summary>
        public static RitCalculator32 PositiveOnly => new RitCalculator32(0x40000000);
        public static RitCalculator32 MaximumRange => new RitCalculator32(0);

        public int GetForkNode(int lower, int upper)
        {
            if (lower > upper) throw new ArgumentException("Interval's lower bound cannot be greater than (after) its upper bound.");

            // This algorithm is copied directly from http://sqlmag.com/t-sql/sql-server-interval-queries .
            // Note that because we're generalising to arbitrary roots it is easier to use iterative logic
            // rather than bithacks.
            var node = Root;
            var step = InitialStep;
            while (step >= 1)
            {
                if(upper < node) node -= step;
                else if(lower > node) node += step;
                else break;
                step /= 2;
            }
            return node;
        }

        public int[] GetLeftNodes(int lower)
        {
            var nodes = new List<int>(31);
            var node = Root;
            var step = InitialStep;
            while (step >= 1)
            {
                if (lower == node) break;
                if (lower < node)
                {
                    node -= step;
                }
                else if (lower > node)
                {
                    nodes.Add(node);
                    node += step;
                }
                step /= 2;
            }
            return nodes.ToArray();
        }

        public int[] GetRightNodes(int upper)
        {
            var nodes = new List<int>(31);
            var node = Root;
            var step = InitialStep;
            while (step >= 1)
            {
                if (upper == node) break;
                if (upper > node)
                {
                    node += step;
                }
                else if (upper < node)
                {
                    nodes.Add(node);
                    node -= step;
                }
                step /=2;
            }
            return nodes.ToArray();
        }

        public RitQuery32 GenerateQuery(int lower, int upper)
        {
            if (lower > upper) throw new ArgumentException("Interval's lower bound cannot be greater than (after) its upper bound.");
            return new RitQuery32 {
                Lower = lower,
                Upper = upper,
                LeftNodes = GetLeftNodes(lower),
                RightNodes = GetRightNodes(upper),
            };
        }
    }
}
