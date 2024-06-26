﻿using Bluewire.NHibernate.Audit.Model;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.Simple;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests
{
    [TestFixture]
    public class AuditModelBuilderTests
    {
        [Test]
        public void CannotAuditEntityTypeWithNoVersionProperty()
        {
            var cfg = new Configuration();
            cfg.DataBaseIntegration(d => { d.Dialect<SQLiteDialect>(); });
            var mapper = new ModelMapper();
            mapper.Class<SimpleEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
            });
            mapper.Class<SimpleEntityAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            Assert.Throws<AuditConfigurationException>(() => new AuditModelBuilder().AddFromConfiguration(cfg));
        }

        [Test]
        public void CanAuditEntityTypeWithMappedUnauditedCollectionProperty()
        {
            var cfg = new Configuration();
            cfg.DataBaseIntegration(d => { d.Dialect<SQLiteDialect>(); });
            var mapper = new ModelMapper();
            mapper.Class<SimpleEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
            });
            mapper.Class<SimpleEntityAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            Assert.Throws<AuditConfigurationException>(() => new AuditModelBuilder().AddFromConfiguration(cfg));
        }

        [Test]
        public void InverseCollectionsMustBeOmittedFromOptimisticLocking()
        {
            var cfg = new Configuration();
            cfg.DataBaseIntegration(d =>
            {
                d.Dialect<SQLiteDialect>();
            });

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

                        // Should cause an error.
                        c.OptimisticLock(true);

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

            var modelBuilder = new AuditModelBuilder();

            Assert.That(() => modelBuilder.AddFromConfiguration(cfg), Throws.InstanceOf<AuditConfigurationException>().With.Message.Contains("inverse"));
        }
    }
}
