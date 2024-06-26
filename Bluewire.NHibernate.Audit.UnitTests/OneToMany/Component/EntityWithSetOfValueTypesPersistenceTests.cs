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
    public class EntityWithSetOfValueTypesPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public EntityWithSetOfValueTypesPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }


        [Test]
        public void SavingCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        new ComponentType { Integer = 7, String = "6" }
                    }
                };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithSetOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithSetOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.AreEquivalent(new[] { 2, 7 }, auditedCollection.Select(c => c.Value.Integer).ToArray());
            }
        }

        [Test]
        public void SavingEmptyCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfValueTypes { Id = 42 };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithSetOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithSetOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.IsEmpty(auditedCollection);
            }
        }

        [Test]
        public void UpdatingCollectionElementIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var modifiable = new ComponentType { Integer = 7, String = "6" };
                var entity = new EntityWithSetOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        modifiable
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                modifiable.String = "8";
                session.Flush();

                var auditedEntities = session.Query<EntityWithSetOfValueTypesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithSetOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(3));

                var originalFirstElement = auditedCollection[0];
                var originalSecondElement = auditedCollection[1];
                var updatedSecondElement = auditedCollection[2];

                Assert.AreEqual(2, originalFirstElement.Value.Integer);
                Assert.AreEqual(7, originalSecondElement.Value.Integer);
                Assert.AreEqual(7, updatedSecondElement.Value.Integer);

                Assert.AreEqual("8", updatedSecondElement.Value.String);

                Assert.AreEqual(originalSecondElement.EndDatestamp, updatedSecondElement.StartDatestamp);
                Assert.IsNull(originalFirstElement.EndDatestamp);
                Assert.IsNull(updatedSecondElement.EndDatestamp);
            }
        }

        [Test]
        public void AddingElementToCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var addable = new ComponentType { Integer = 7, String = "8" };
                var entity = new EntityWithSetOfValueTypes
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

                entity.Values.Add(addable);
                session.Flush();

                var auditedEntities = session.Query<EntityWithSetOfValueTypesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithSetOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var originalKeyA = auditedCollection[0];
                var insertedKeyB = auditedCollection[1];

                Assert.AreEqual("8", insertedKeyB.Value.String);
                Assert.AreNotEqual(originalKeyA.StartDatestamp, insertedKeyB.StartDatestamp);
                Assert.IsNull(insertedKeyB.EndDatestamp);
            }
        }

        [Test]
        public void RemovingElementFromCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var removable = new ComponentType { Integer = 7, String = "8" };
                var entity = new EntityWithSetOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        removable
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Values.Remove(removable);
                session.Flush();

                var audited = session.Query<EntityWithSetOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();

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
                var entity = new EntityWithSetOfValueTypes
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

                session.Delete(entity);
                session.Flush();

                var audited = session.Query<EntityWithSetOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));
                Assert.That(audited.Select(x => x.EndDatestamp), Has.All.Not.Null);
            }
        }

        [Test]
        public void DeletingOwnerEntityWithoutLoadingLazyCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfValueTypes
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

                session.Clear();
                entity = session.Get<EntityWithSetOfValueTypes>(entity.Id);
                Assume.That(NHibernateUtil.IsInitialized(entity.Values), Is.False);
                session.Delete(entity);
                Assume.That(NHibernateUtil.IsInitialized(entity.Values), Is.False);
                session.Flush();

                var audited = session.Query<EntityWithSetOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));
                Assert.That(audited.Select(x => x.EndDatestamp), Has.All.Not.Null);
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithSetOfValueTypes>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Set(i => i.Values,
                    c => { c.Table("EntityWithSetOfValueTypesValues"); },
                    r => r.Component(c =>
                    {
                        c.Property(x => x.String);
                        c.Property(x => x.Integer);
                    }));
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithSetOfValueTypesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            mapper.Class<EntityWithSetOfValueTypesValuesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.StartDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.EndDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
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
                x.CreateMap<EntityWithSetOfValueTypes, EntityWithSetOfValueTypesAuditHistory>().IgnoreHistoryMetadata();
            });
            new AuditConfigurer(auditEntryFactory, new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}
