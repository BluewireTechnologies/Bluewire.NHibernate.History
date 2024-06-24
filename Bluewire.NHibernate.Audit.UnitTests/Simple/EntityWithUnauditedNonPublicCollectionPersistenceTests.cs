using System.Linq;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.ManyToMany;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Simple
{
    [TestFixture]
    public class EntityWithUnauditedNonPublicCollectionPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public EntityWithUnauditedNonPublicCollectionPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }

        [Test]
        public void CanSaveEntityWithUnauditedNonPublicCollection()
        {
            using (var session = db.CreateSession())
            {
                var a = new ReferencableEntity { String = "2" };
                var entity = new EntityWithUnauditedNonPublicCollection
                {
                    Id = 42,
                    Value = "Initial value",
                    Entities = { a }
                };
                session.Save(entity);
                session.Flush();

                var audited = session.Query<EntityWithUnauditedNonPublicCollectionAuditHistory>().Single(h => h.Id == 42);

                Assert.AreEqual(42, audited.Id);
                Assert.AreEqual(entity.VersionId, audited.VersionId);
                Assert.AreEqual(entity.Value, audited.Value);
                Assert.AreEqual(null, audited.PreviousVersionId);
                Assert.AreEqual(AuditedOperation.Add, audited.AuditedOperation);
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

            mapper.Class<EntityWithUnauditedNonPublicCollection>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Property(i => i.Value);
                e.List<ReferencableEntity>("Entities_NonPublic",
                    c =>
                    {
                        c.Table("EntityWithUnauditedNonPublicCollectionEntities");
                        c.Cascade(Cascade.All); // Not required by audit. Convenience for testing.
                    },
                    r => r.ManyToMany());
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithUnauditedNonPublicCollectionAuditHistory>(e =>
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

            var auditEntryFactory = new AutoAuditEntryFactory(x =>
            {
                x.CreateMap<EntityWithUnauditedNonPublicCollection, EntityWithUnauditedNonPublicCollectionAuditHistory>().IgnoreHistoryMetadata();
            });
            new AuditConfigurer(auditEntryFactory, new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}
