using System;
using System.Linq;
using Bluewire.Common.Time;
using Bluewire.IntervalTree;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.IntervalTree
{
    [TestFixture]
    public class EntityWithIntervalPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();
        private RitExpectations expectations = new RitExpectations(new PerMinuteSnapshotIntervalTree32());

        public EntityWithIntervalPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }


        [Test]
        public void CreatingEntityGeneratesValidRitEntry()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithInterval { Id = 42, Value = "Initial value" };
                session.Save(entity);
                session.Flush();
                session.Clear();

                var audited = session.Query<EntityWithIntervalHistory>().Single(h => h.Id == 42);

                Assert.AreEqual(42, audited.Id);

                expectations.VerifyCurrentRitEntry(audited.RitMinutes, clock.Now);
            }
        }

        [Test]
        public void UpdatingEntityGeneratesValidRitEntry_AndUpdatesPreviousEntryUpperBound_ButLeavesNodeStale()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithInterval { Id = 42, Value = "Initial value" };
                session.Save(entity);
                session.Flush();
                var initialVersion = entity.VersionId;

                clock.Advance(TimeSpan.FromMinutes(5));

                entity.Value = "Updated value";
                session.Flush();
                Assume.That(entity.VersionId, Is.Not.EqualTo(initialVersion));
                session.Clear();

                var audited = session.Query<EntityWithIntervalHistory>().Where(h => h.Id == 42).OrderBy(h => h.AuditDatestamp).ToList();

                expectations.VerifyCurrentRitEntry(audited.Last().RitMinutes, clock.Now);
                expectations.VerifyPreviousRitEntry(audited.First().RitMinutes, clock.Now);
            }
        }

        [Test]
        public void DeletingEntityGeneratesValidRitEntry_AndUpdatesPreviousEntryUpperBound_ButLeavesNodeStale()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithInterval { Id = 42, Value = "Initial value" };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromMinutes(5));

                session.Delete(entity);
                session.Flush();
                session.Clear();
                
                var audited = session.Query<EntityWithIntervalHistory>().Where(h => h.Id == 42).OrderBy(h => h.AuditDatestamp).ToList();

                Assert.That(audited.Last().AuditedOperation, Is.EqualTo(AuditedOperation.Delete));

                expectations.VerifyCurrentRitEntry(audited.Last().RitMinutes, clock.Now);
                expectations.VerifyPreviousRitEntry(audited.First().RitMinutes, clock.Now);
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithInterval>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Property(i => i.Value);
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithIntervalHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.Value);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Component(i => i.RitMinutes, r => {
                    r.Property(i => i.Lower);
                    r.Property(i => i.Node);
                    r.Property(i => i.Upper);
                    r.Property(i => i.Status, p => p.Type<RitStatusEnumType>());
                });
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new AuditConfigurer(new DynamicAuditEntryFactory(), new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}
