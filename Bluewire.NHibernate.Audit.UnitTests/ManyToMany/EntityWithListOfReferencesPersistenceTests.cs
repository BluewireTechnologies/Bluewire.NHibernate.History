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
    public class EntityWithListOfReferencesPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public EntityWithListOfReferencesPersistenceTests()
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

                var entity = new EntityWithListOfReferences
                {
                    Id = 42,
                    Entities = { a, b }
                };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithListOfReferencesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfReferencesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.AreEqual(new[] { 0, 1 }, auditedCollection.Select(c => c.Key).ToArray());
            }
        }

        [Test]
        public void SavingEmptyCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfReferences { Id = 42 };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithListOfReferencesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfReferencesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
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

                var entity = new EntityWithListOfReferences
                {
                    Id = 42,
                    Entities = { a }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Entities.Add(b);
                session.Flush();

                var auditedEntities = session.Query<EntityWithListOfReferencesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithListOfReferencesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var originalIndex0 = auditedCollection[0];
                var insertedIndex1 = auditedCollection[1];

                Assert.AreEqual(b.Id, insertedIndex1.Value);
                Assert.AreNotEqual(originalIndex0.StartDatestamp, insertedIndex1.StartDatestamp);
                Assert.IsNull(insertedIndex1.EndDatestamp);
            }
        }

        [Test]
        public void ReorderingCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var a = new ReferencableEntity { String = "2" };
                var b = new ReferencableEntity { String = "8" };

                var entity = new EntityWithListOfReferences
                {
                    Id = 42,
                    Entities = { a, b }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                var temp = entity.Entities[0];
                entity.Entities[0] = entity.Entities[1];
                entity.Entities[1] = temp;
                session.Flush();

                var auditedEntities = session.Query<EntityWithListOfReferencesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithListOfReferencesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(4));

                var originalIndex0 = auditedCollection[0];
                var originalIndex1 = auditedCollection[1];
                var reorderedIndex0 = auditedCollection[2];
                var reorderedIndex1 = auditedCollection[3];

                Assert.AreEqual(b.Id, reorderedIndex0.Value);
                Assert.AreEqual(a.Id, reorderedIndex1.Value);

                Assert.IsNotNull(originalIndex0.EndDatestamp);
                Assert.IsNotNull(originalIndex1.EndDatestamp);
                Assert.IsNull(reorderedIndex0.EndDatestamp);
                Assert.IsNull(reorderedIndex1.EndDatestamp);
            }
        }

        [Test]
        public void RemovingElementFromEndOfCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var a = new ReferencableEntity { String = "2" };
                var b = new ReferencableEntity { String = "8" };

                var entity = new EntityWithListOfReferences
                {
                    Id = 42,
                    Entities = { a, b }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Entities.RemoveAt(1);
                session.Flush();

                var audited = session.Query<EntityWithListOfReferencesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));

                var item = audited.ElementAt(1);
                Assert.AreEqual(b.Id, item.Value);
                Assert.IsNotNull(item.EndDatestamp);
            }
        }

        [Test]
        public void RemovingElementFromStartOfCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var a = new ReferencableEntity { String = "2" };
                var b = new ReferencableEntity { String = "8" };

                var entity = new EntityWithListOfReferences
                {
                    Id = 42,
                    Entities = { a, b }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Entities.RemoveAt(0);
                session.Flush();

                var auditedEntities = session.Query<EntityWithListOfReferencesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithListOfReferencesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(3));

                var originalIndex0 = auditedCollection[0];
                var originalIndex1 = auditedCollection[1];
                var remainingIndex0 = auditedCollection[2];

                Assert.AreEqual(b.Id, remainingIndex0.Value);

                Assert.IsNotNull(originalIndex0.EndDatestamp);
                Assert.IsNotNull(originalIndex1.EndDatestamp);
                Assert.IsNull(remainingIndex0.EndDatestamp);
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

            mapper.Class<EntityWithListOfReferences>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.List(i => i.Entities,
                    c => {
                        c.Table("EntityWithListOfReferencesEntities");
                        c.Cascade(Cascade.All); // Not required by audit. Convenience for testing.
                    },
                    r => r.ManyToMany());
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithListOfReferencesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            mapper.Class<EntityWithListOfReferencesEntitiesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.StartDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.EndDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.Key);
                e.Property(i => i.OwnerId);
                e.Property(i => i.Value);
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            var auditEntryFactory = new AutoAuditEntryFactory(x =>
            {
                x.CreateMap<EntityWithListOfReferences, EntityWithListOfReferencesAuditHistory>().IgnoreHistoryMetadata();
            });
            new AuditConfigurer(auditEntryFactory, new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}
