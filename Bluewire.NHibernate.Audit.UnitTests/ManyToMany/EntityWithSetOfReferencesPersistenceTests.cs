using System;
using System.Linq;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.ManyToMany
{
    [TestFixture]
    public class EntityWithSetOfReferencesPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public EntityWithSetOfReferencesPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }


        [Test]
        public void SavingCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var a = new ReferencableEntity { String = "2" };
                var b = new ReferencableEntity { String = "6" };

                var entity = new EntityWithSetOfReferences
                {
                    Id = 42,
                    Entities = { a, b }
                };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithSetOfReferencesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithSetOfReferencesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.AreEqual(new[] { a.Id, b.Id }, auditedCollection.Select(c => c.Value).ToArray());
            }
        }

        [Test]
        public void SavingEmptyCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfReferences { Id = 42 };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithSetOfReferencesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithSetOfReferencesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.IsEmpty(auditedCollection);
            }
        }

        [Test]
        public void AddingElementToCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var a = new ReferencableEntity { String = "2" };
                var b = new ReferencableEntity { String = "8" };

                var entity = new EntityWithSetOfReferences
                {
                    Id = 42,
                    Entities = { a }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Entities.Add(b);
                session.Flush();

                var auditedEntities = session.Query<EntityWithSetOfReferencesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithSetOfReferencesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var originalIndex0 = auditedCollection[0];
                var insertedIndex1 = auditedCollection[1];

                Assert.AreEqual(b.Id, insertedIndex1.Value);
                Assert.AreNotEqual(originalIndex0.StartDatestamp, insertedIndex1.StartDatestamp);
                Assert.IsNull(insertedIndex1.EndDatestamp);
            }
        }

        [Test]
        public void RemovingElementFromCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var a = new ReferencableEntity { String = "2" };
                var b = new ReferencableEntity { String = "8" };

                var entity = new EntityWithSetOfReferences
                {
                    Id = 42,
                    Entities = { a, b }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Entities.Remove(b);
                session.Flush();

                var audited = session.Query<EntityWithSetOfReferencesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));

                var item = audited.Single(i => i.Value == b.Id);
                Assert.AreEqual(b.Id, item.Value);
                Assert.IsNotNull(item.EndDatestamp);
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<ReferencableEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.String);
            });

            mapper.Class<EntityWithSetOfReferences>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Set(i => i.Entities,
                    c => {
                        c.Table("EntityWithSetOfReferencesEntities");
                        c.Cascade(Cascade.All); // Not required by audit. Convenience for testing.
                    },
                    r => r.ManyToMany());
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithSetOfReferencesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            mapper.Class<EntityWithSetOfReferencesEntitiesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.StartDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.EndDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.OwnerId);
                e.Property(i => i.Value);
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            var auditEntryFactory = new AutoAuditEntryFactory(x =>
            {
                x.CreateMap<EntityWithSetOfReferences, EntityWithSetOfReferencesAuditHistory>().IgnoreHistoryMetadata();
            });
            new AuditConfigurer(auditEntryFactory, new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}
