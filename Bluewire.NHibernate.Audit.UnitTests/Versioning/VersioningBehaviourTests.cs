using Bluewire.NHibernate.Audit.UnitTests.Simple;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Versioning
{
    /// <summary>
    /// NHibernate's optimistic concurrency handles version numbers internally and ignores the
    /// value of the version property on the entity, which is used only for persistence mapping.
    /// Flushing an entity fails with a StaleObjectStateException if the version in the session
    /// does not match the version in the database.
    /// 
    /// We extend NHibernate's optimistic concurrency to also fail with a StaleObjectStateException
    /// if the version on the entity does not match the version in the session. This is useful
    /// when an operation may cross multiple sessions and may not carry the entire entity state
    /// between them, necessitating a read-modify-write to save it.
    /// </summary>
    [TestFixture]
    public class VersioningBehaviourTests
    {
        [Test]
        public void CannotSaveRefetchedEntityWithOverriddenVersion()
        {
            using (var db = PersistentDatabase.Configure(Configure))
            {
                var entity = new VersionedEntity { Id = 42 };
                using (var session = db.CreateSession())
                {
                    session.Save(entity);
                    session.Flush();
                }
                Assume.That(entity.VersionId, Is.GreaterThan(0));

                using (var session = db.CreateSession())
                {
                    entity = session.Get<VersionedEntity>(42);
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
                var entity = new VersionedEntity { Id = 42 };
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
                var entity = new VersionedEntity { Id = 42 };
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
                var entity = new VersionedEntity { Id = 42 };
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

        private static void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<VersionedEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Version(i => i.VersionId, v => { });
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
            new AuditConfigurer(new DynamicAuditEntryFactory()).IntegrateWithNHibernate(cfg);
        }
    }

    public class VersionedEntity
    {
        public virtual int Id { get; set; }
        public virtual int VersionId { get; set; }
        public virtual string Value { get; set; }
    }
}

