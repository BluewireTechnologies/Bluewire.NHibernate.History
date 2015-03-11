using System;
using System.Linq;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Entity
{
    [TestFixture]
    public class EntityWithSetOfEntityTypesPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public EntityWithSetOfEntityTypesPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }


        [Test]
        public void SavingCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfEntityTypes
                {
                    Id = 42,
                    Entities =
                    {
                        new OneToManyEntity { Id = 2, Value = "2" },
                        new OneToManyEntity { Id = 7, Value = "6" },
                    }
                };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithSetOfEntityTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithSetOfEntityTypesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.AreEquivalent(new[] { 2, 7 }, auditedCollection.Select(c => c.Value).ToArray());
            }
        }

        [Test]
        public void SavingEmptyCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfEntityTypes { Id = 42 };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithSetOfEntityTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithSetOfEntityTypesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.IsEmpty(auditedCollection);
            }
        }

        [Test]
        public void UpdatingCollectionElementIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfEntityTypes
                {
                    Id = 42,
                    Entities =
                    {
                        new OneToManyEntity { Id = 2, Value = "2" },
                        new OneToManyEntity { Id = 7, Value = "6" },
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Entities.Single(e => e.Id == 7).Value = "8";
                session.Flush();

                var auditedEntity = session.Query<EntityWithSetOfEntityTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithSetOfEntityTypesEntitiesAuditHistory>().ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var auditedCollectionEntries = session.Query<OneToManyEntityAuditHistory>().ToList();
                Assert.That(auditedCollectionEntries.Count, Is.EqualTo(3));

                var originalIndex0 = auditedCollectionEntries[0];
                var originalIndex1 = auditedCollectionEntries[1];
                var updatedIndex1 = auditedCollectionEntries[2];

                Assert.AreEqual("8", updatedIndex1.Value);
                Assert.AreEqual(AuditedOperation.Update, updatedIndex1.AuditedOperation);
                Assert.AreEqual(originalIndex1.VersionId, updatedIndex1.PreviousVersionId);
                Assert.Greater(updatedIndex1.AuditDatestamp, originalIndex1.AuditDatestamp);
                Assert.AreEqual(originalIndex1.Id, updatedIndex1.Id);
                Assert.AreNotEqual(originalIndex0.Id, updatedIndex1.Id);
            }
        }

        [Test]
        public void AddingElementToCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfEntityTypes
                {
                    Id = 42,
                    Entities =
                    {
                        new OneToManyEntity { Id = 2, Value = "2" }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Entities.Add(new OneToManyEntity { Id = 7, Value = "8" });
                session.Flush();

                var auditedEntity = session.Query<EntityWithSetOfEntityTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithSetOfEntityTypesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var auditedCollectionEntries = session.Query<OneToManyEntityAuditHistory>().ToList();

                var insertedIndex1 = auditedCollection[1];

                Assert.AreEqual(AuditedOperation.Add, auditedCollectionEntries[1].AuditedOperation);
                Assert.AreNotEqual(auditedCollectionEntries[0].AuditDatestamp, auditedCollectionEntries[1].AuditDatestamp);
                Assert.AreEqual(auditedCollectionEntries[1].Id, insertedIndex1.Value);
            }
        }

        [Test]
        public void RemovingElementFromCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfEntityTypes
                {
                    Id = 42,
                    Entities =
                    {
                        new OneToManyEntity { Id = 2, Value = "2" },
                        new OneToManyEntity { Id = 7, Value = "8" },
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Entities.Remove(entity.Entities.Single(e => e.Id == 7));
                session.Flush();

                var auditedCollection = session.Query<EntityWithSetOfEntityTypesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var auditedCollectionEntries = session.Query<OneToManyEntityAuditHistory>().ToList();
                Assert.That(auditedCollectionEntries.Count, Is.EqualTo(2));
                
                Assert.AreEqual(7, auditedCollection.ElementAt(1).Value);
                Assert.IsNotNull(auditedCollection.ElementAt(1).EndDatestamp);
                // Note that removing the item from the collection does not delete it.
            }
        }
        
        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithSetOfEntityTypes>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Set(i => i.Entities, c =>
                {
                    c.Cascade(Cascade.All);
                }, m => m.OneToMany());
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<OneToManyEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Property(i => i.Value);
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithSetOfEntityTypesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            mapper.Class<OneToManyEntityAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Property(i => i.Value);
                e.Mutable(false);
            });
            mapper.Class<EntityWithSetOfEntityTypesEntitiesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.StartDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.EndDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.OwnerId);
                e.Property(i => i.Value);
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new AuditConfigurer(new DynamicAuditEntryFactory(), new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}