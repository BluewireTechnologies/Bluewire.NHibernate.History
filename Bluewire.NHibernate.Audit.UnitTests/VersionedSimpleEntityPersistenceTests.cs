using System;
using System.Diagnostics;
using System.Linq;
using AutoMapper;
using AutoMapper.Mappers;
using Bluewire.NHibernate.Audit.Model;
using Bluewire.NHibernate.Audit.Support;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NHibernate.Type;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests
{
    [TestFixture]
    public class VersionedSimpleEntityPersistenceTests
    {
        private TemporaryDatabase db;

        public VersionedSimpleEntityPersistenceTests()
        {
            
            db = TemporaryDatabase.Configure(Configure);
        }


        [Test]
        public void SavingSimpleEntityIsAudited()
        {
            const int ID = 42;

            using (var session = db.CreateSession())
            {
                var entity = new VersionedSimpleEntity { Id = ID, Value = "Initial value" };
                session.Save(entity);
                session.Flush();

                var audited = session.Query<VersionedSimpleEntityAuditHistory>().Single(h => h.Id == ID);

                Assert.AreEqual(ID, audited.Id);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(entity.Value, audited.Value);
                Assert.AreEqual(null, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Add, audited.AuditedOperation);
            }
        }

        [Test]
        public void UpdatingSimpleEntityIsAudited()
        {
            const int ID = 42;

            using (var session = db.CreateSession())
            {
                var entity = new VersionedSimpleEntity { Id = ID, Value = "Initial value" };
                session.Save(entity);
                session.Flush();
                var initialVersion = entity.VersionId;

                entity.Value = "Updated value";
                session.Flush();
                Assume.That(entity.VersionId, Is.Not.EqualTo(initialVersion));

                var audited = session.Query<VersionedSimpleEntityAuditHistory>().SingleOrDefault(h => h.Id == ID && h.VersionId == entity.VersionId);

                Assert.AreEqual(ID, audited.Id);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(entity.Value, audited.Value);
                Assert.AreEqual(initialVersion, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Update, audited.AuditedOperation);
            }
        }

        [Test]
        public void DeletingSimpleEntityIsAudited()
        {
            const int ID = 42;

            using (var session = db.CreateSession())
            {
                var entity = new VersionedSimpleEntity { Id = ID, Value = "Initial value" };
                session.Save(entity);
                session.Flush();

                session.Delete(entity);
                session.Flush();

                var audited = session.Query<VersionedSimpleEntityAuditHistory>().Where(h => h.Id == ID).ToList();

                Assert.That(audited.Count, Is.EqualTo(2));

                var deletion = audited.ElementAt(1);

                Assert.AreEqual(ID, deletion.Id);
                Assert.IsNull(deletion.VersionId);
                Assert.AreEqual(entity.Value, deletion.Value);
                Assert.AreEqual(entity.VersionId, deletion.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Delete, deletion.AuditedOperation);
            }
        }

        private static void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<VersionedSimpleEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Property(i => i.Value);
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<VersionedSimpleEntityAuditHistory>(e =>
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

    class DynamicAuditEntryFactory : IAuditEntryFactory
    {
        public void AssertConfigurationIsValid()
        {
        }

        public bool CanCreate(Type entityType, Type auditEntryType)
        {
            return true;
        }

        public IAuditHistory Create(object entity, Type entityType, Type auditEntryType)
        {
            return (IAuditHistory)Mapper.DynamicMap(entity, entityType, auditEntryType);
        }
    }
}