using System;
using System.Linq;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.ManyToOne
{
    [TestFixture]
    public class EntityWithManyToOnePersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public EntityWithManyToOnePersistenceTests()
        {
            
            db = TemporaryDatabase.Configure(Configure);
        }


        [Test]
        public void SavingEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var unaudited = new UnauditedEntity { Id = 2 };
                session.Save(unaudited);
                var entity = new EntityWithManyToOne { Id = 42, Reference = unaudited };
                session.Save(entity);
                session.Flush();

                var audited = session.Query<EntityWithManyToOneAuditHistory>().Single(h => h.Id == 42);

                Assert.AreEqual(42, audited.Id);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(unaudited.Id, audited.ReferenceId);
                Assert.AreEqual(null, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Add, audited.AuditedOperation);
            }
        }

        [Test]
        public void SavingEntityWithNoReferenceIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithManyToOne { Id = 42, Reference = null };
                session.Save(entity);
                session.Flush();

                var audited = session.Query<EntityWithManyToOneAuditHistory>().Single(h => h.Id == 42);

                Assert.AreEqual(42, audited.Id);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(null, audited.ReferenceId);
                Assert.AreEqual(null, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Add, audited.AuditedOperation);
            }
        }

        [Test]
        public void UpdatingSimpleEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var firstUnaudited = new UnauditedEntity { Id = 2 };
                session.Save(firstUnaudited);
                var entity = new EntityWithManyToOne { Id = 42, Reference = firstUnaudited };
                session.Save(entity);
                session.Flush();
                var initialVersion = entity.VersionId;

                clock.Advance(TimeSpan.FromSeconds(1));

                var secondUnaudited = new UnauditedEntity { Id = 4 };
                session.Save(secondUnaudited);
                entity.Reference = secondUnaudited;
                session.Flush();
                Assume.That(entity.VersionId, Is.Not.EqualTo(initialVersion));

                var audited = session.Query<EntityWithManyToOneAuditHistory>().Single(h => h.Id == 42 && h.VersionId == entity.VersionId);

                Assert.AreEqual(42, audited.Id);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(secondUnaudited.Id, audited.ReferenceId);
                Assert.AreEqual(initialVersion, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Update, audited.AuditedOperation);
            }
        }

        [Test]
        public void DeletingSimpleEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var unaudited = new UnauditedEntity { Id = 2 };
                session.Save(unaudited);
                var entity = new EntityWithManyToOne { Id = 42, Reference = unaudited };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                session.Delete(entity);
                session.Flush();

                var audited = session.Query<EntityWithManyToOneAuditHistory>().Where(h => h.Id == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));

                var deletion = audited.ElementAt(1);

                Assert.AreEqual(42, deletion.Id);
                Assert.IsNull(deletion.VersionId);
                Assert.AreEqual(unaudited.Id, deletion.ReferenceId);
                Assert.AreEqual(entity.VersionId, deletion.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Delete, deletion.AuditedOperation);
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<UnauditedEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
            });
            mapper.Class<EntityWithManyToOne>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.ManyToOne(i => i.Reference);
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithManyToOneAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.ReferenceId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new AuditConfigurer(new DynamicAuditEntryFactory(), new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}