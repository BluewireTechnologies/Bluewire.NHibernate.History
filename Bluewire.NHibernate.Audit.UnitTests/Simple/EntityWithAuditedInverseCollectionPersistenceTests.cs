using System.Linq;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Simple
{
    [TestFixture]
    public class EntityWithInverseCollectionPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public EntityWithInverseCollectionPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }

        [Test]
        public void CanSaveEntityWithInverseCollection()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithAuditedInverseCollection
                {
                    Id = 42,
                    Value = "Initial value",
                };
                session.Save(entity);
                entity.Entities.Add(new InverseReferencableEntity { String = "2", OwnerId = entity.Id });
                session.Flush();

                var audited = session.Query<EntityWithAuditedInverseCollectionAuditHistory>().Single(h => h.Id == 42);

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
            mapper.Class<InverseReferencableEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.String);
                e.Property(i => i.OwnerId);
            });

            mapper.Class<EntityWithAuditedInverseCollection>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Property(i => i.Value);
                e.List(i => i.Entities,
                    c =>
                    {
                        c.Table("EntityWithAuditedInverseCollectionEntities");
                        c.Inverse(true);

                        // Inverse-mapped collections MUST specify this as 'false'.
                        // If they do not, later versions of NHibernate may increment the entity's version in
                        // response to a change in the collection's contents, which breaks history as the entity
                        // has not actually changed.
                        c.OptimisticLock(false);

                        c.Cascade(Cascade.All); // Not required by audit. Convenience for testing.
                        c.Key(k => k.Column("OwnerId"));
                    },
                    r => r.OneToMany());
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithAuditedInverseCollectionAuditHistory>(e =>
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
                x.CreateMap<EntityWithAuditedInverseCollection, EntityWithAuditedInverseCollectionAuditHistory>().IgnoreHistoryMetadata();
            });
            new AuditConfigurer(auditEntryFactory, new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}
