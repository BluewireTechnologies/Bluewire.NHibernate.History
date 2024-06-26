using System;
using System.Linq;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany.Component
{
    [TestFixture]
    public class EntityWithMapOfValueTypesPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public EntityWithMapOfValueTypesPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }


        [Test]
        public void SavingCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithMapOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        { "A", new ComponentType { Integer = 2, String = "2" } },
                        { "B", new ComponentType { Integer = 7, String = "6" } }
                    }
                };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithMapOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithMapOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.AreEquivalent(new[] { "A", "B" }, auditedCollection.Select(c => c.Key).ToArray());
            }
        }

        [Test]
        public void SavingEmptyCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithMapOfValueTypes { Id = 42 };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithMapOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithMapOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.IsEmpty(auditedCollection);
            }
        }

        [Test]
        public void UpdatingCollectionElementIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithMapOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        { "A", new ComponentType { Integer = 2, String = "2" } },
                        { "B", new ComponentType { Integer = 7, String = "6" } }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Values["B"].String = "8";
                session.Flush();

                var auditedEntities = session.Query<EntityWithMapOfValueTypesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithMapOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(3));

                var originalKeyA = auditedCollection[0];
                var originalKeyB = auditedCollection[1];
                var updatedKeyB = auditedCollection[2];

                Assert.AreEqual("A", originalKeyA.Key);
                Assert.AreEqual("B", originalKeyB.Key);
                Assert.AreEqual("B", updatedKeyB.Key);

                Assert.AreEqual("8", updatedKeyB.Value.String);

                Assert.AreEqual(originalKeyB.EndDatestamp, updatedKeyB.StartDatestamp);
                Assert.IsNull(originalKeyA.EndDatestamp);
                Assert.IsNull(updatedKeyB.EndDatestamp);
            }
        }

        [Test]
        public void AddingElementToCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithMapOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        { "A", new ComponentType { Integer = 2, String = "2" } }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Values.Add("B", new ComponentType { Integer = 7, String = "8" });
                session.Flush();

                var auditedEntities = session.Query<EntityWithMapOfValueTypesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithMapOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var originalKeyA = auditedCollection[0];
                var insertedKeyB = auditedCollection[1];

                Assert.AreEqual("8", insertedKeyB.Value.String);
                Assert.AreNotEqual(originalKeyA.StartDatestamp, insertedKeyB.StartDatestamp);
                Assert.IsNull(insertedKeyB.EndDatestamp);
            }
        }

        [Test]
        public void SwappingValuesIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithMapOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        { "A", new ComponentType { Integer = 2, String = "2" } },
                        { "B", new ComponentType { Integer = 7, String = "8" } }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                var temp = entity.Values["A"];
                entity.Values["A"] = entity.Values["B"];
                entity.Values["B"] = temp;
                session.Flush();

                var auditedEntities = session.Query<EntityWithMapOfValueTypesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithMapOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(4));

                var originalKeyA = auditedCollection[0];
                var originalKeyB = auditedCollection[1];
                var reorderedKeyA = auditedCollection[2];
                var reorderedKeyB = auditedCollection[3];

                Assert.AreEqual("8", reorderedKeyA.Value.String);
                Assert.AreEqual("2", reorderedKeyB.Value.String);

                Assert.IsNotNull(originalKeyA.EndDatestamp);
                Assert.IsNotNull(originalKeyB.EndDatestamp);
                Assert.IsNull(reorderedKeyA.EndDatestamp);
                Assert.IsNull(reorderedKeyB.EndDatestamp);
            }
        }

        [Test]
        public void RemovingElementFromCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithMapOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        { "A", new ComponentType { Integer = 2, String = "2" } },
                        { "B", new ComponentType { Integer = 7, String = "8" } }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Values.Remove("B");
                session.Flush();

                var audited = session.Query<EntityWithMapOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));

                var item = audited.ElementAt(1);
                Assert.AreEqual("8", item.Value.String);
                Assert.IsNotNull(item.EndDatestamp);
            }
        }

        [Test]
        public void DeletingOwnerEntityDeletedCollection()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithMapOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        { "A", new ComponentType { Integer = 2, String = "2" } },
                        { "B", new ComponentType { Integer = 7, String = "8" } }
                    }
                };

                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                session.Delete(entity);
                session.Flush();

                var audited = session.Query<EntityWithMapOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));
                Assert.That(audited.Select(x => x.EndDatestamp), Has.All.Not.Null);
            }
        }

        [Test]
        public void DeletingOwnerEntityWithoutLoadingLazyCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithMapOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        { "A", new ComponentType { Integer = 2, String = "2" } },
                        { "B", new ComponentType { Integer = 7, String = "8" } }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                session.Clear();
                entity = session.Get<EntityWithMapOfValueTypes>(entity.Id);
                Assume.That(NHibernateUtil.IsInitialized(entity.Values), Is.False);
                session.Delete(entity);
                Assume.That(NHibernateUtil.IsInitialized(entity.Values), Is.False);
                session.Flush();

                var audited = session.Query<EntityWithMapOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));
                Assert.That(audited.Select(x => x.EndDatestamp), Has.All.Not.Null);
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithMapOfValueTypes>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Map(i => i.Values,
                    c => { c.Table("EntityWithMapOfValueTypesValues"); },
                    r => r.Component(c =>
                    {
                        c.Property(x => x.String);
                        c.Property(x => x.Integer);
                    }));
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithMapOfValueTypesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            mapper.Class<EntityWithMapOfValueTypesValuesAuditHistory>(e =>
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

            var auditEntryFactory = new AutoAuditEntryFactory(x =>
            {
                x.CreateMap<EntityWithMapOfValueTypes, EntityWithMapOfValueTypesAuditHistory>().IgnoreHistoryMetadata();
            });
            new AuditConfigurer(auditEntryFactory, new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}
