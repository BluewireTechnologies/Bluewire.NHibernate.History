using System.Linq;
using Bluewire.IntervalTree.UnitTests.Util;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.IntervalTree.UnitTests
{
    [TestFixture]
    public class RitQueryIntegrationTests
    {
        private TemporaryDatabase db;

        public RitQueryIntegrationTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }

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

        [Test]
        public void QueryingSnapshotReturnsExactlyOneCorrectMatch()
        {
            using (var session = db.CreateSession())
            {
                var set = new [] {
                    CreateRitEntity(10, 34),    // Fork = 8
                    CreateRitEntity(34, 36),    // N/A, never crosses a RIT boundary.
                    CreateRitEntity(36, 45)     // Fork = 10
                };
                foreach (var s in  set) session.Save(s);
                session.Flush();

                // At most one match:
                for (var i = 0; i < 64; i++)
                {
                    Assert.That(QueryForSnapshot(session, i).Count(), Is.LessThanOrEqualTo(1));
                }

                // Verify specific cases:
                Assert.That(QueryForSnapshot(session, 10), Is.Empty); // 10 is rounded down to a RIT boundary which is before item 0 'exists'.
                Assert.That(QueryForSnapshot(session, 32).Single(), Is.EqualTo(set[0]));
                Assert.That(QueryForSnapshot(session, 34).Single(), Is.EqualTo(set[0]));
                Assert.That(QueryForSnapshot(session, 36).Single(), Is.EqualTo(set[2]));
            }
        }

        private IQueryable<RitEntity> QueryForSnapshot(ISession session, int snapshot)
        {
            var query = tree.GenerateQuery(snapshot, snapshot);
            return session.Query<RitEntity>().Overlapping(e => e.Rit, query);
        }

        private RitEntity CreateRitEntity(int start, int end)
        {
            return new RitEntity {
                Start = start,
                End = end,
                Rit = tree.CalculateNode(start, end)
            };
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<RitEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Start);
                e.Property(i => i.End);
                e.Component(m => m.Rit, m => {
                    m.Property(x => x.Lower, x => x.Column("ritLower"));
                    m.Property(x => x.Node, x => x.Column("ritNode"));
                    m.Property(x => x.Upper, x => x.Column("ritUpper"));
                    m.Property(x => x.Status, x => {
                        x.Column(_ => {
                            _.Name("ritStatus");
                            _.SqlType("int");
                        });
                        x.NotNullable(true);
                        x.Type(NHibernateUtil.Int32);
                    });
                });
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
        }
    }

    class RitEntity
    {
        public virtual int Id { get; set; }

        public virtual int Start { get; set; }
        public virtual int End { get; set; }
        public virtual RitEntry32 Rit { get; set; }

        public override string ToString()
        {
            return $"{Rit}";
        }
    }
}
