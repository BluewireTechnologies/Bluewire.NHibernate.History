using System;
using System.Linq;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToOne
{
    [TestFixture]
    public class EntityWithAuditedCascadeDeleteOneToOnePersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public EntityWithAuditedCascadeDeleteOneToOnePersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }


        [Test]
        public void SavingEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithAuditedOneToOne { Id = 42, Reference = new AuditedChildEntity { Value = 2 } };
                session.Save(entity);
                session.Flush();

                var audited = session.Query<EntityWithAuditedOneToOneAuditHistory>().Single(h => h.Id == 42);

                Assert.AreEqual(42, audited.Id);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(42, audited.ReferenceId);
                Assert.AreEqual(2, audited.ReferenceValue);
                Assert.AreEqual(null, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Add, audited.AuditedOperation);
            }
        }

        [Test]
        public void SavingEntityWithNoReferenceIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithAuditedOneToOne { Id = 42, Reference = null };
                session.Save(entity);
                session.Flush();

                var audited = session.Query<EntityWithAuditedOneToOneAuditHistory>().Single(h => h.Id == 42);

                Assert.AreEqual(42, audited.Id);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(null, audited.ReferenceId);
                Assert.AreEqual(null, audited.ReferenceValue);
                Assert.AreEqual(null, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Add, audited.AuditedOperation);
            }
        }

        [Test]
        public void RemovingAuditedChildEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithAuditedOneToOne { Id = 42, Reference = new AuditedChildEntity { Value = 2 } };
                session.Save(entity);
                session.Flush();
                var initialVersion = entity.VersionId;

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Reference = null;
                session.Flush();
                Assume.That(entity.VersionId, Is.Not.EqualTo(initialVersion));

                var audited = session.Query<EntityWithAuditedOneToOneAuditHistory>().Single(h => h.Id == 42 && h.VersionId == entity.VersionId);

                Assert.AreEqual(42, audited.Id);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(null, audited.ReferenceId);
                Assert.AreEqual(null, audited.ReferenceValue);
                Assert.AreEqual(initialVersion, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Update, audited.AuditedOperation);
            }
        }

        [Test]
        public void UpdatingReferencedEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithAuditedOneToOne { Id = 42, Reference = new AuditedChildEntity { Value = 2 } };
                session.Save(entity);
                session.Flush();
                var initialVersion = entity.VersionId;

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Reference.Value = 4;
                session.Flush();
                Assume.That(entity.VersionId, Is.Not.EqualTo(initialVersion));

                var audited = session.Query<EntityWithAuditedOneToOneAuditHistory>().Single(h => h.Id == 42 && h.VersionId == entity.VersionId);

                Assert.AreEqual(42, audited.Id);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(42, audited.ReferenceId);
                Assert.AreEqual(4, audited.ReferenceValue);
                Assert.AreEqual(initialVersion, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Update, audited.AuditedOperation);
            }
        }

        [Test]
        public void DeletingSimpleEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithAuditedOneToOne { Id = 42, Reference = new AuditedChildEntity { Value = 2 } };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                session.Delete(entity);
                session.Flush();

                var audited = session.Query<EntityWithAuditedOneToOneAuditHistory>().Where(h => h.Id == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));

                var deletion = audited.ElementAt(1);

                Assert.AreEqual(42, deletion.Id);
                Assert.IsNull(deletion.VersionId);
                Assert.AreEqual(42, deletion.ReferenceId);
                Assert.AreEqual(2, deletion.ReferenceValue);
                Assert.AreEqual(entity.VersionId, deletion.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Delete, deletion.AuditedOperation);
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<AuditedChildEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(Generators.Foreign<AuditedChildEntity>(x => x.__Owner)));
                e.Property(i => i.Value);
                e.OneToOne(i => i.__Owner, x => x.Constrained(true));
            });
            mapper.Class<EntityWithAuditedOneToOne>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.OneToOne(i => i.Reference, i => i.Cascade(Cascade.All | Cascade.DeleteOrphans));
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithAuditedOneToOneAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.ReferenceId);
                e.Property(i => i.ReferenceValue);
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
