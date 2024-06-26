using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;
using System.Collections.Generic;

namespace Bluewire.NHibernate.Audit.UnitTests.Versioning
{
    /// <summary>
    /// NHibernate's optimistic concurrency increments the owning entity's version
    /// number when updating its collections.
    /// </summary>
    [TestFixture]
    public class CollectionVersioningBehaviourTests
    {
        [Test]
        public void CannotSaveConflictingCollection()
        {
            using (var db = PersistentDatabase.Configure(Configure))
            {
                var entity = new EntityWithCollection { Id = 42, List = { "Existing" } };
                using (var session = db.CreateSession())
                {
                    session.Save(entity);
                    session.Flush();
                }

                using (var sessionA = db.CreateSession())
                using (var sessionB = db.CreateSession())
                {
                    var entityA = sessionA.Get<EntityWithCollection>(42);
                    entityA.List[0] = "A";

                    var entityB = sessionB.Get<EntityWithCollection>(42);
                    entityB.List[0] = "B";

                    sessionA.Flush();

                    Assert.Throws<StaleObjectStateException>(() =>
                    {
                        sessionB.Flush();
                    });
                }
                using (var sessionC = db.CreateSession())
                {
                    var entityC = sessionC.Get<EntityWithCollection>(42);

                    Assume.That(entityC.VersionId, Is.Not.EqualTo(entity.VersionId)); // Assume that the entity's version was used as a guard.
                    Assert.That(entityC.List[0], Is.EqualTo("A")); // Session A won.
                }
            }
        }

        private static void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithCollection>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Version(i => i.VersionId, v => { });
                e.List(i => i.List,
                     c => { c.Table("ListEntries"); },
                     r => r.Element()
                     );
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            var auditEntryFactory = new AutoAuditEntryFactory(x => { });
            new AuditConfigurer(auditEntryFactory, new ClockAuditDatestampProvider(new Clock())).IntegrateWithNHibernate(cfg);
        }
    }

    public class EntityWithCollection
    {
        public EntityWithCollection()
        {
            List = new List<string>();
        }

        public virtual int Id { get; set; }
        public virtual int VersionId { get; set; }
        public virtual IList<string> List { get; protected set; }
    }
}
