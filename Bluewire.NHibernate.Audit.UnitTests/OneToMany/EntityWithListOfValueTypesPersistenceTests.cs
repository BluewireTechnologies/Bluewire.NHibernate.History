using System.Linq;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.ManyToOne;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.OneToMany
{
    [TestFixture]
    public class EntityWithListOfValueTypesPersistenceTests
    {
        private TemporaryDatabase db;

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

                var auditedCollection = session.Query<EntityWithListOfValueTypesValuesAuditHistory>().Where(h => h.EntityWithListOfValueTypesId == 42).ToList();
                CollectionAssert.AreEqual(new[] { 0, 1 }, auditedCollection.Select(c => c.Index).ToArray());
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

                var auditedCollection = session.Query<EntityWithListOfValueTypesValuesAuditHistory>().Where(h => h.EntityWithListOfValueTypesId == 42).ToList();
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

                entity.Values[1].String = "8";
                session.Flush();

                var auditedEntity = session.Query<EntityWithListOfValueTypesAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfValueTypesValuesAuditHistory>().Where(h => h.EntityWithListOfValueTypesId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(3));

                var originalIndex0 = auditedCollection[0];
                var originalIndex1 = auditedCollection[1];
                var updatedIndex1 = auditedCollection[2];

                Assert.AreEqual("8", updatedIndex1.String);
                Assert.AreEqual(originalIndex1.EndDatestamp, updatedIndex1.StartDatestamp);
                Assert.IsNull(originalIndex0.EndDatestamp);
                Assert.IsNull(updatedIndex1.EndDatestamp);
            }
        }

        [Test, Ignore]
        public void AddingElementToCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var unaudited = new UnauditedEntity { Id = 2 };
                session.Save(unaudited);
                var entity = new EntityWithManyToOne { Id = 42, Reference = unaudited };
                session.Save(entity);
                session.Flush();

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

        [Test, Ignore]
        public void RemovingElementFromCollectionIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var unaudited = new UnauditedEntity { Id = 2 };
                session.Save(unaudited);
                var entity = new EntityWithManyToOne { Id = 42, Reference = unaudited };
                session.Save(entity);
                session.Flush();

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

        private static void Configure(Configuration cfg)
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
                e.Property(i => i.Index);
                e.Property(i => i.EntityWithListOfValueTypesId);
                e.Property(i => i.String);
                e.Property(i => i.Integer);
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new AuditConfigurer(new DynamicAuditEntryFactory()).IntegrateWithNHibernate(cfg);
        }
    }
}