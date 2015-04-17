using System;
using System.Linq;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Simple;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Inheritance
{
    [TestFixture]
    public class DerivedEntityPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public DerivedEntityPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }


        [Test]
        public void SavingDerivedEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new DerivedEntity { Id = 42, Value = "Initial value" };
                session.Save(entity);
                session.Flush();

                var audited = session.Query<BaseEntityAuditHistory>().Single(h => h.Id == 42);

                Assert.AreEqual(42, audited.Id);
                Assert.IsTrue(audited is DerivedEntityAuditHistory);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(entity.Value, ((DerivedEntityAuditHistory)audited).Value);
                Assert.AreEqual(null, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Add, audited.AuditedOperation);
            }
        }

        [Test]
        public void UpdatingSimpleEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new DerivedEntity { Id = 42, Value = "Initial value" };
                session.Save(entity);
                session.Flush();
                var initialVersion = entity.VersionId;

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Value = "Updated value";
                session.Flush();
                Assume.That(entity.VersionId, Is.Not.EqualTo(initialVersion));

                var audited = session.Query<BaseEntityAuditHistory>().Single(h => h.Id == 42 && h.VersionId == entity.VersionId);

                Assert.AreEqual(42, audited.Id);
                Assert.IsTrue(audited is DerivedEntityAuditHistory);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(entity.Value, ((DerivedEntityAuditHistory)audited).Value);
                Assert.AreEqual(initialVersion, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Update, audited.AuditedOperation);
            }
        }

        [Test]
        public void DeletingSimpleEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new DerivedEntity { Id = 42, Value = "Initial value" };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                session.Delete(entity);
                session.Flush();

                var audited = session.Query<BaseEntityAuditHistory>().Where(h => h.Id == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));

                var deletion = audited.ElementAt(1);

                Assert.AreEqual(42, deletion.Id);
                Assert.IsTrue(deletion is DerivedEntityAuditHistory);
                Assert.IsNull(deletion.VersionId);
                Assert.AreEqual(entity.Value, ((DerivedEntityAuditHistory)deletion).Value);
                Assert.AreEqual(entity.VersionId, deletion.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Delete, deletion.AuditedOperation);
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<BaseEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Subclass<DerivedEntity>(e =>
            {
                e.Property(i => i.Value);
            });
            mapper.Class<BaseEntityAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            mapper.Subclass<DerivedEntityAuditHistory>(e =>
            {
                e.Property(i => i.Value);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new AuditConfigurer(new DynamicAuditEntryFactory(), new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}