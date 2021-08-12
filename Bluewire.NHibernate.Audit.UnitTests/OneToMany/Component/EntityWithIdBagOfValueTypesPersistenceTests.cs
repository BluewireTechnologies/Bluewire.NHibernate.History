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
    public class EntityWithIdBagOfValueTypesPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public EntityWithIdBagOfValueTypesPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }

        [Test]
        public void SavingCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithIdBagOfValueTypes
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

                var auditedEntity = session.Query<EntityWithIdBagOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithIdBagOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.AreEquivalent(new[] { 2, 7 }, auditedCollection.Select(c => c.Value.Integer).ToArray());
            }
        }

        [Test]
        public void SavingEmptyCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithIdBagOfValueTypes { Id = 42 };
                session.Save(entity);
                session.Flush();

                var auditedEntity = session.Query<EntityWithIdBagOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithIdBagOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                CollectionAssert.IsEmpty(auditedCollection);
            }
        }

        [Test]
        public void UpdatingCollectionElementIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var modifiable = new ComponentType { Integer = 7, String = "6" };
                var entity = new EntityWithIdBagOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        modifiable,
                        new ComponentType { Integer = 7, String = "6" },
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                modifiable.String = "8";
                session.Flush();

                var auditedEntities = session.Query<EntityWithIdBagOfValueTypesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithIdBagOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(4));

                var originalElements = auditedCollection.Take(3).ToList();
                var updatedElement = auditedCollection.ElementAt(3);

                Assert.That(originalElements.Select(e => e.Value), Is.EquivalentTo(new [] {
                    new ComponentType { Integer = 2, String = "2" },
                    new ComponentType { Integer = 7, String = "6" },
                    new ComponentType { Integer = 7, String = "6" },
                }));

                Assert.AreEqual("8", updatedElement.Value.String);

                var original = originalElements.Single(e => e.EndDatestamp != null);
                Assert.AreEqual(original.EndDatestamp, updatedElement.StartDatestamp);
            }
        }

        [Test]
        public void AddingElementToCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var addable = new ComponentType { Integer = 7, String = "8" };
                var entity = new EntityWithIdBagOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        new ComponentType { Integer = 7, String = "8" },
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Values.Add(addable);
                session.Flush();

                var auditedEntities = session.Query<EntityWithIdBagOfValueTypesAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithIdBagOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(3));

                var originalElements = auditedCollection.Take(2).ToList();
                var insertedElement = auditedCollection.ElementAt(2);

                Assert.AreEqual("8", insertedElement.Value.String);
                Assert.That(originalElements.Select(s => s.StartDatestamp), Does.Not.Contains(insertedElement.StartDatestamp));
                Assert.IsNull(insertedElement.EndDatestamp);
            }
        }

        [Test]
        public void RemovingElementFromCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var removable = new ComponentType { Integer = 7, String = "8" };
                var entity = new EntityWithIdBagOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        new ComponentType { Integer = 7, String = "8" },
                        removable
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                entity.Values.Remove(removable);
                session.Flush();

                var audited = session.Query<EntityWithIdBagOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(3));

                var item = audited.Single(i => i.EndDatestamp != null);
                Assert.AreEqual("8", item.Value.String);
            }
        }

        [Test]
        public void DeletingOwnerEntityDeletedCollection()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithIdBagOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        new ComponentType { Integer = 2, String = "2" },
                        new ComponentType { Integer = 7, String = "8" }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                session.Delete(entity);
                session.Flush();

                var audited = session.Query<EntityWithIdBagOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(3));
                Assert.That(audited.Select(x => x.EndDatestamp), Has.All.Not.Null);
            }
        }

        [Test]
        public void DeletingOwnerEntityWithoutLoadingLazyCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithIdBagOfValueTypes
                {
                    Id = 42,
                    Values =
                    {
                        new ComponentType { Integer = 2, String = "2" },
                        new ComponentType { Integer = 2, String = "2" },
                        new ComponentType { Integer = 7, String = "8" }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromSeconds(1));

                session.Clear();
                entity = session.Get<EntityWithIdBagOfValueTypes>(entity.Id);
                Assume.That(NHibernateUtil.IsInitialized(entity.Values), Is.False);
                session.Delete(entity);
                Assume.That(NHibernateUtil.IsInitialized(entity.Values), Is.False);
                session.Flush();

                var audited = session.Query<EntityWithIdBagOfValueTypesValuesAuditHistory>().Where(h => h.OwnerId == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(3));
                Assert.That(audited.Select(x => x.EndDatestamp), Has.All.Not.Null);
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithIdBagOfValueTypes>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.IdBag(i => i.Values,
                    c => {
                        c.Table("EntityWithIdBagOfValueTypesValues");
                        c.Id(x => {
                            x.Generator(new HighLowGeneratorDef());
                            x.Column("id");
                        });
                    },
                    r => r.Component(c =>
                    {
                        c.Property(x => x.String);
                        c.Property(x => x.Integer);
                    }));
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithIdBagOfValueTypesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            mapper.Class<EntityWithIdBagOfValueTypesValuesAuditHistory>(e =>
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

            new AuditConfigurer(new DynamicAuditEntryFactory(), new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}
