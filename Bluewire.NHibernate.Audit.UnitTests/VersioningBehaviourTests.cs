using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests
{
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
            new AuditConfigurer().IntegrateWithNHibernate(cfg);
        }
    }

    public class VersionedEntity
    {
        public virtual int Id { get; set; }
        public virtual int VersionId { get; set; }
        public virtual string Value { get; set; }
    }
}

