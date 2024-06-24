using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Attributes;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.OneToMany.Element;
using Bluewire.NHibernate.Audit.UnitTests.OneToMany.Entity;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Versioning
{
    [TestFixture]
    public class RelationVersioningAuditBehaviourTests
    {
        [Test]
        public void VersionIdIncrementCausedByCollectionModificationCausesAHistoryInsertion()
        {
            using (var db = PersistentDatabase.Configure(Configure))
            {
                var entity = new EntityWithPropertyAndListOfPrimitiveTypes { Id = 42, Values = { "Existing" } };
                using (var session = db.CreateSession())
                {
                    session.Save(entity);
                    session.Flush();
                    var originalVersionId = entity.VersionId;

                    entity.Values.Add("Item");
                    session.Flush();
                    Assume.That(entity.VersionId, Is.Not.EqualTo(originalVersionId));

                    entity.Property = "Test";
                    session.Flush();

                    Verify.HistoryChain(session);
                }
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithPropertyAndListOfPrimitiveTypes>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.List(i => i.Values,
                    c => { c.Table("EntityWithPropertyAndListOfPrimitiveTypesValues"); },
                    r => r.Element()
                    );
                e.Property(c => c.Property);
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithPropertyAndListOfPrimitiveTypesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            mapper.Class<EntityWithPropertyAndListOfPrimitiveTypesValuesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.StartDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.EndDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.Key);
                e.Property(i => i.OwnerId);
                e.Property(i => i.Value);
                e.Mutable(false);
            });
            var hbm = mapper.CompileMappingForAllExplicitlyAddedEntities();
            cfg.AddMapping(hbm);

            var auditEntryFactory = new AutoAuditEntryFactory(x =>
            {
                x.CreateMap<EntityWithPropertyAndListOfPrimitiveTypes, EntityWithPropertyAndListOfPrimitiveTypesAuditHistory>().IgnoreHistoryMetadata();
            });
            new AuditConfigurer(auditEntryFactory, new ClockAuditDatestampProvider(new Clock())).IntegrateWithNHibernate(cfg);
        }
    }

    [AuditableEntity(typeof(EntityWithPropertyAndListOfPrimitiveTypesAuditHistory))]
    public class EntityWithPropertyAndListOfPrimitiveTypes
    {
        public EntityWithPropertyAndListOfPrimitiveTypes()
        {
            Values = new List<string>();
        }

        public virtual int Id { get; set; }
        [AuditableRelation(typeof(EntityWithPropertyAndListOfPrimitiveTypesValuesAuditHistory))]
        public virtual IList<string> Values { get; protected set; }
        public virtual string Property { get; set; }
        public virtual int VersionId { get; set; }
    }

    public class EntityWithPropertyAndListOfPrimitiveTypesAuditHistory : EntityAuditHistoryBase<int, int>
    {
        public virtual string Property { get; protected set; }
    }

    public class EntityWithPropertyAndListOfPrimitiveTypesValuesAuditHistory : KeyedRelationAuditHistoryEntry<int, int, string>
    {
    }
}
