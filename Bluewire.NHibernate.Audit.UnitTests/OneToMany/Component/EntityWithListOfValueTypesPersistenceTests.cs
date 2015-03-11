using System;
using System.Linq;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Component
{
    [TestFixture]
    public class EntityWithListOfValueTypesPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public EntityWithListOfValueTypesPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }


        [Test]
        public void SavingCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        new ComponentType { Integer = 7, String = "6" },
                    }
                };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithListOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.AreEqual(new[] { 0, 1 }, auditedCollection.Select(c => c.Key).ToArray());
            }
        }

        [Test]
        public void SavingEmptyCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfValueTypes { Id = 42 };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithListOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.IsEmpty(auditedCollection);
            }
        }

        [Test]
        public void UpdatingCollectionElementIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        new ComponentType { Integer = 7, String = "6" },
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Values[1].String = "8";
                session.Flush();

                var auditedEntity = session.Query<EntityWithListOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(3));

                var originalIndex0 = auditedCollection[0];
                var originalIndex1 = auditedCollection[1];
                var updatedIndex1 = auditedCollection[2];

                Assert.AreEqual("8", updatedIndex1.Value.String);
                Assert.AreEqual(originalIndex1.EndDatestamp, updatedIndex1.StartDatestamp);
                Assert.IsNull(originalIndex0.EndDatestamp);
                Assert.IsNull(updatedIndex1.EndDatestamp);
            }
        }

        [Test]
        public void AddingElementToCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Values.Add(new ComponentType { Integer = 7, String = "8" });
                session.Flush();

                var auditedEntity = session.Query<EntityWithListOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var originalIndex0 = auditedCollection[0];
                var insertedIndex1 = auditedCollection[1];

                Assert.AreEqual("8", insertedIndex1.Value.String);
                Assert.AreNotEqual(originalIndex0.StartDatestamp, insertedIndex1.StartDatestamp);
                Assert.IsNull(insertedIndex1.EndDatestamp);
            }
        }

        [Test]
        public void InsertingNullElementIntoCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Values.Insert(0, null); // Note that appending nulls to a list is completely ignored by NHibernate.
                session.Flush();

                var auditedEntity = session.Query<EntityWithListOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var originalIndex0 = auditedCollection[0];
                var updatedIndex1 = auditedCollection[1];

                // Inserting a null is recorded as updating the key values of subsequent entries.
                Assert.AreEqual("2", updatedIndex1.Value.String);
                Assert.AreNotEqual(originalIndex0.StartDatestamp, updatedIndex1.StartDatestamp);
                Assert.IsNull(updatedIndex1.EndDatestamp);
            }
        }

        [Test]
        public void ReorderingCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        new ComponentType { Integer = 7, String = "8" }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                var temp = entity.Values[0];
                entity.Values[0] = entity.Values[1];
                entity.Values[1] = temp;
                session.Flush();

                var auditedEntity = session.Query<EntityWithListOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(4));

                var originalIndex0 = auditedCollection[0];
                var originalIndex1 = auditedCollection[1];
                var reorderedIndex0 = auditedCollection[2];
                var reorderedIndex1 = auditedCollection[3];

                Assert.AreEqual("8", reorderedIndex0.Value.String);
                Assert.AreEqual("2", reorderedIndex1.Value.String);

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
                var entity = new EntityWithListOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        new ComponentType { Integer = 7, String = "8" }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Values.RemoveAt(1);
                session.Flush();

                var audited = session.Query<EntityWithListOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));

                var item = audited.ElementAt(1);
                Assert.AreEqual("8", item.Value.String);
                Assert.IsNotNull(item.EndDatestamp);
            }
        }

        [Test]
        public void RemovingElementFromStartOfCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        new ComponentType { Integer = 7, String = "8" }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Values.RemoveAt(0);
                session.Flush();

                var auditedEntity = session.Query<EntityWithListOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(3));

                var originalIndex0 = auditedCollection[0];
                var originalIndex1 = auditedCollection[1];
                var remainingIndex0 = auditedCollection[2];

                Assert.AreEqual("8", remainingIndex0.Value.String);

                Assert.IsNotNull(originalIndex0.EndDatestamp);
                Assert.IsNotNull(originalIndex1.EndDatestamp);
                Assert.IsNull(remainingIndex0.EndDatestamp);
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithListOfValueTypes>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.List(i => i.Values,
                    c => { c.Table("EntityWithListOfValueTypesValues"); },
                    r => r.Component(c =>
                    {
                        c.Property(x => x.String);
                        c.Property(x => x.Integer);
                    }));
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithListOfValueTypesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            mapper.Class<EntityWithListOfValueTypesValuesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.StartDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.EndDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.Key);
                e.Property(i => i.OwnerId);
                e.Component(i => i.Value, c =>
                {
                    c.Property(i => i.String);
                    c.Property(i => i.Integer);
                });
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new AuditConfigurer(new DynamicAuditEntryFactory(), clock).IntegrateWithNHibernate(cfg);
        }
    }
}