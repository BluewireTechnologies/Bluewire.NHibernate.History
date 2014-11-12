using System.Linq;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Simple
{
    [TestFixture]
    public class SimpleEntityPersistenceTests
    {
        private TemporaryDatabase db;

        public SimpleEntityPersistenceTests()
        {
            
            db = TemporaryDatabase.Configure(Configure);
        }


        [Test]
        public void SavingSimpleEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new SimpleEntity { Id = 42, Value = "Initial value" };
                session.Save(entity);
                session.Flush();

                var audited = session.Query<SimpleEntityAuditHistory>().Single(h => h.Id == 42);

                Assert.AreEqual(42, audited.Id);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(entity.Value, audited.Value);
                Assert.AreEqual(null, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Add, audited.AuditedOperation);
            }
        }

        [Test]
        public void UpdatingSimpleEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new SimpleEntity { Id = 42, Value = "Initial value" };
                session.Save(entity);
                session.Flush();
                var initialVersion = entity.VersionId;

                entity.Value = "Updated value";
                session.Flush();
                Assume.That(entity.VersionId, Is.Not.EqualTo(initialVersion));

                var audited = session.Query<SimpleEntityAuditHistory>().Single(h => h.Id == 42 && h.VersionId == entity.VersionId);

                Assert.AreEqual(42, audited.Id);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(entity.Value, audited.Value);
                Assert.AreEqual(initialVersion, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Update, audited.AuditedOperation);
            }
        }

        [Test]
        public void DeletingSimpleEntityIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new SimpleEntity { Id = 42, Value = "Initial value" };
                session.Save(entity);
                session.Flush();

                session.Delete(entity);
                session.Flush();

                var audited = session.Query<SimpleEntityAuditHistory>().Where(h => h.Id == 42).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));

                var deletion = audited.ElementAt(1);

                Assert.AreEqual(42, deletion.Id);
                Assert.IsNull(deletion.VersionId);
                Assert.AreEqual(entity.Value, deletion.Value);
                Assert.AreEqual(entity.VersionId, deletion.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Delete, deletion.AuditedOperation);
            }
        }

        private static void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<SimpleEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Property(i => i.Value);
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<SimpleEntityAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.Value);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new AuditConfigurer(new DynamicAuditEntryFactory()).IntegrateWithNHibernate(cfg);
        }
    }
}