using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Attributes;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Versioning
{
    [TestFixture]
    public class VersioningAuditBehaviourTests
    {
        [Test]
        public void CannotSaveRefetchedEntityWithOverriddenVersion()
        {
            using (var db = PersistentDatabase.Configure(Configure))
            {
                var entity = new EntityWithProperty { Id = 42 };
                using (var session = db.CreateSession())
                {
                    session.Save(entity);
                    session.Flush();
                }
                Assume.That(entity.VersionId, Is.GreaterThan(0));

                using (var session = db.CreateSession())
                {
                    entity = session.Get<EntityWithProperty>(42);
                    entity.VersionId = 0;
                    entity.Value = "Test";

                    Assert.Throws<StaleObjectStateException>(() =>
                    {
                        session.Save(entity);
                        session.Flush();
                    });
                }
            }
        }

        [Test]
        public void CannotSaveReassociatedEntityWithOverriddenPreviousVersion()
        {
            using (var db = PersistentDatabase.Configure(Configure))
            {
                var entity = new EntityWithProperty { Id = 42 };
                using (var session = db.CreateSession())
                {
                    session.Save(entity);
                    session.Flush();
                }
                Assume.That(entity.VersionId, Is.GreaterThan(0));

                entity.VersionId = 0;
                entity.Value = "Test";

                using (var session = db.CreateSession())
                {
                    Assert.Throws<StaleObjectStateException>(() =>
                    {
                        session.Update(entity);
                        session.Flush();
                    });
                }
            }
        }

        [Test]
        public void CanSaveReassociatedEntityWithOverriddenCurrentVersion()
        {
            using (var db = PersistentDatabase.Configure(Configure))
            {
                var entity = new EntityWithProperty { Id = 42 };
                using (var session = db.CreateSession())
                {
                    session.Save(entity);
                    session.Flush();
                }
                Assume.That(entity.VersionId, Is.GreaterThan(0));

                entity.VersionId = entity.VersionId;
                entity.Value = "Test";

                using (var session = db.CreateSession())
                {
                    session.Update(entity);
                    session.Flush();
                }
            }
        }

        [Test]
        public void CanSaveReassociatedEntityWithoutOverriddingVersion()
        {
            using (var db = PersistentDatabase.Configure(Configure))
            {
                var entity = new EntityWithProperty { Id = 42 };
                using (var session = db.CreateSession())
                {
                    session.Save(entity);
                    session.Flush();
                }
                Assume.That(entity.VersionId, Is.GreaterThan(0));

                entity.Value = "Test";

                using (var session = db.CreateSession())
                {
                    session.Update(entity);
                    session.Flush();
                }
            }
        }

        [Test]
        public void VersionConflictIsDetectedBeforeSavingHistoryRecord()
        {
            using (var db = PersistentDatabase.Configure(Configure))
            {
                var entity = new EntityWithProperty { Id = 42 };
                using (var session = db.CreateSession())
                {
                    session.Save(entity);
                    session.Flush();
                }

                using (var sessionA = db.CreateSession())
                using (var sessionB = db.CreateSession())
                {
                    var entityA = sessionA.Get<EntityWithProperty>(42);
                    var entityB = sessionB.Get<EntityWithProperty>(42);

                    entityA.Value = "Test";
                    sessionA.Update(entityA);
                    sessionA.Flush();

                    entityB.Value = "Modified";
                    Assert.Throws<StaleObjectStateException>(() => {
                        // The unique constraint on PreviousVersionId will cause these to fail if the audit record is written.
                        sessionB.Update(entityB);
                        sessionB.Flush();
                    });
                }
            }
        }

        private static void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithProperty>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Version(i => i.VersionId, v => { });
                e.Property(i => i.Value);
            });
            mapper.Class<EntityWithPropertyAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId, x => x.Unique(true));
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Property(i => i.Value);
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
            new AuditConfigurer(new DynamicAuditEntryFactory(), new ClockAuditDatestampProvider(new Clock())).IntegrateWithNHibernate(cfg);
        }
    }

    [AuditableEntity(typeof(EntityWithPropertyAuditHistory))]
    public class EntityWithProperty
    {
        public virtual int Id { get; set; }
        public virtual int VersionId { get; set; }
        public virtual string Value { get; set; }
    }

    public class EntityWithPropertyAuditHistory : EntityAuditHistoryBase<int, int>
    {
        public virtual string Value { get; set; }
    }
}
