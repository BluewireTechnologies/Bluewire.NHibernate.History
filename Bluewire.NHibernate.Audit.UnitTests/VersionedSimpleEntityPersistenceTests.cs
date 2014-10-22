using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests
{
    [TestFixture, Ignore]
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
                var entity = new VersionedSimpleEntity { Id = ID };
                using (var transaction = session.BeginTransaction())
                {
                    session.Save(entity);
                    transaction.Commit();
                }

                var audited = session.CreateSQLQuery("select Id, VersionId, PreviousVersionId, AuditedOperation from VersionedSimpleEntityAuditHistory").UniqueResult<object[]>();

                CollectionAssert.AreEqual(new object[] { ID, entity.VersionId, null, AuditedOperation.Add }, audited);
            }
        }

        private static void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<VersionedSimpleEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<VersionedSimpleEntityAuditHistory>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());
            new AuditConfigurer().IntegrateWithNHibernate(cfg);
        }
    }
}