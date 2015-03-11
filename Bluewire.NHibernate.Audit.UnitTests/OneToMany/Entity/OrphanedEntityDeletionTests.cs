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
    public class OrphanedEntityDeletionTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public OrphanedEntityDeletionTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }

        [Test]
        public void DeletionOfOrphanedElementIsAudited()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfEntityTypes
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

                entity.Entities.Remove(entity.Entities.Single(e => e.Id == 7));
                session.Flush();

                var deleted = session.Get<OneToManyEntity>(7);
                Assert.IsNull(deleted);

                var auditedCollectionEntries = session.Query<OneToManyEntityAuditHistory>().Where(e => e.Id == 7).ToList();
                
                Assert.That(auditedCollectionEntries, Has.Exactly(1).Matches<OneToManyEntityAuditHistory>(e => e.AuditedOperation == AuditedOperation.Delete));
            }
        }
        
        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithSetOfEntityTypes>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Set(i => i.Entities, c =>
                {
                    c.Cascade(Cascade.All | Cascade.DeleteOrphans);
                }, m => m.OneToMany());
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<OneToManyEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Property(i => i.Value);
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithSetOfEntityTypesAuditHistory>(e =>
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
            mapper.Class<EntityWithSetOfEntityTypesEntitiesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.StartDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.EndDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.OwnerId);
                e.Property(i => i.Value);
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new AuditConfigurer(new DynamicAuditEntryFactory(), new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}