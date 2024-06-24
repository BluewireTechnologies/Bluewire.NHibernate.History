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
    public class EntityWithListOfEntityTypesPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public EntityWithListOfEntityTypesPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }


        [Test]
        public void SavingCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfEntityTypes
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

                var auditedEntity = session.Query<EntityWithListOfEntityTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfEntityTypesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.AreEqual(new[] { 0, 1 }, auditedCollection.Select(c => c.Key).ToArray());
            }
        }

        [Test]
        public void SavingEmptyCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfEntityTypes { Id = 42 };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithListOfEntityTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfEntityTypesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.IsEmpty(auditedCollection);
            }
        }

        [Test]
        public void UpdatingCollectionElementIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfEntityTypes
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

                entity.Entities[1].Value = "8";
                session.Flush();

                var auditedEntity = session.Query<EntityWithListOfEntityTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfEntityTypesEntitiesAuditHistory>().ToList();
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
                var entity = new EntityWithListOfEntityTypes
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

                var auditedEntities = session.Query<EntityWithListOfEntityTypesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithListOfEntityTypesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var auditedCollectionEntries = session.Query<OneToManyEntityAuditHistory>().ToList();

                var insertedIndex1 = auditedCollection[1];

                Assert.AreEqual(1, insertedIndex1.Key);
                Assert.AreEqual(AuditedOperation.Add, auditedCollectionEntries[1].AuditedOperation);
                Assert.AreNotEqual(auditedCollectionEntries[0].AuditDatestamp, auditedCollectionEntries[1].AuditDatestamp);
                Assert.AreEqual(auditedCollectionEntries[1].Id, insertedIndex1.Value);
            }
        }

        [Test]
        public void InsertingNullElementIntoCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfEntityTypes
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

                entity.Entities.Insert(0, null); // Note that appending nulls to a list is completely ignored by NHibernate.
                session.Flush();

                var auditedEntities = session.Query<EntityWithListOfEntityTypesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithListOfEntityTypesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var originalIndex0 = auditedCollection[0];
                var updatedIndex1 = auditedCollection[1];

                // Inserting a null is recorded as updating the key values of subsequent entries.
                Assert.AreEqual(2, updatedIndex1.Value);
                Assert.AreNotEqual(originalIndex0.StartDatestamp, updatedIndex1.StartDatestamp);
                Assert.IsNull(updatedIndex1.EndDatestamp);
            }
        }

        [Test]
        public void ReorderingCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfEntityTypes
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

                var temp = entity.Entities[0];
                entity.Entities[0] = entity.Entities[1];
                entity.Entities[1] = temp;
                session.Flush();

                var auditedEntities = session.Query<EntityWithListOfEntityTypesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithListOfEntityTypesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(4));

                var auditedCollectionEntries = session.Query<OneToManyEntityAuditHistory>().ToList();
                Assert.That(auditedCollectionEntries.Count, Is.EqualTo(2));

                var reorderedIndex0 = auditedCollection[2];
                var reorderedIndex1 = auditedCollection[3];

                Assert.AreEqual(auditedCollectionEntries[1].Id, reorderedIndex0.Value);
                Assert.AreEqual(0, reorderedIndex0.Key);
                Assert.AreEqual(auditedCollectionEntries[0].Id, reorderedIndex1.Value);
                Assert.AreEqual(1, reorderedIndex1.Key);
            }
        }

        [Test]
        public void RemovingElementFromEndOfCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfEntityTypes
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

                entity.Entities.RemoveAt(1);
                session.Flush();

                var auditedCollection = session.Query<EntityWithListOfEntityTypesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var auditedCollectionEntries = session.Query<OneToManyEntityAuditHistory>().ToList();
                Assert.That(auditedCollectionEntries.Count, Is.EqualTo(2));

                Assert.AreEqual(7, auditedCollection.ElementAt(1).Value);
                Assert.IsNotNull(auditedCollection.ElementAt(1).EndDatestamp);
                // Note that removing the item from the collection does not delete it.
            }
        }

        [Test]
        public void RemovingElementFromStartOfCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfEntityTypes
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

                entity.Entities.RemoveAt(0);
                session.Flush();

                var auditedEntities = session.Query<EntityWithListOfEntityTypesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithListOfEntityTypesEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(3));

                var auditedCollectionEntries = session.Query<OneToManyEntityAuditHistory>().ToList();
                Assert.That(auditedCollectionEntries.Count, Is.EqualTo(2));

                var removedIndex0 = auditedCollection.Single(o => o.Key == 0 && o.EndDatestamp.HasValue);
                var remainingIndex0 = auditedCollection.Single(o => o.Key == 0 && !o.EndDatestamp.HasValue);

                Assert.AreEqual(7, remainingIndex0.Value);

                Assert.AreEqual(2, removedIndex0.Value);
                // Note that removing the item from the collection does not delete it.
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithListOfEntityTypes>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.List(i => i.Entities, c =>
                {
                    c.Index(i => i.Column("idx"));
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
            mapper.Class<EntityWithListOfEntityTypesAuditHistory>(e =>
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
            mapper.Class<EntityWithListOfEntityTypesEntitiesAuditHistory>(e =>
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
                x.CreateMap<OneToManyEntity, OneToManyEntityAuditHistory>().IgnoreHistoryMetadata();
                x.CreateMap<EntityWithListOfEntityTypes, EntityWithListOfEntityTypesAuditHistory>().IgnoreHistoryMetadata();
            });
            new AuditConfigurer(auditEntryFactory, new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}
