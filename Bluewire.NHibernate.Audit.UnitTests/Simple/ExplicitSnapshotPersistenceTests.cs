using System;
using System.Linq;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using Bluewire.NHibernate.Audit.Query;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Simple
{
    [TestFixture]
    public class ExplicitSnapshotPersistenceTests
    {
        private MockClock clock = new MockClock();

        [Test]
        public void CanAcquireAuditDatestampConsistentWithCurrentState()
        {
            IAuditInfo auditInfo;
            using (var db = CreateDatabase(out auditInfo))
            {
                DateTimeOffset datestamp;
                using (var session = db.CreateSession())
                {
                    var entity = new SimpleEntity { Id = 42, Value = "Initial value" };
                    session.Save(entity);
                    session.Flush();

                    clock.Advance(TimeSpan.FromSeconds(1));

                    entity.Value = "Updated";

                    datestamp = auditInfo.CommitSnapshot(session);

                    clock.Advance(TimeSpan.FromSeconds(1));

                    session.Flush();
                }

                using (var session = db.CreateSession())
                {
                    var snapshot = session.At(datestamp).GetModel<SimpleEntityAuditHistory, int>().Get(42);
                    Assert.That(snapshot.Value, Is.EqualTo("Updated"));
                }
            }
        }

        private PersistentDatabase CreateDatabase(out IAuditInfo auditInfo)
        {
            IAuditInfo a = null;
            var db = PersistentDatabase.Configure(c => Configure(c, out a));
            auditInfo = a;
            return db;
        }

        private void Configure(Configuration cfg, out IAuditInfo auditInfo)
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

            var configurer = new AuditConfigurer(new DynamicAuditEntryFactory(), new ClockAuditDatestampProvider(clock));
            auditInfo = configurer.IntegrateWithNHibernate(cfg);
        }
    }
}
