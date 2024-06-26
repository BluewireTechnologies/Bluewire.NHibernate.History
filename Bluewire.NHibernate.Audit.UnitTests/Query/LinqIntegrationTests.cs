using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bluewire.Common.Time;
using Bluewire.NHibernate.Audit.Attributes;
using Bluewire.NHibernate.Audit.Query;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.ManyToOne;
using Bluewire.NHibernate.Audit.UnitTests.OneToMany.Element;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Query
{
    [TestFixture]
    public class LinqIntegrationTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();

        public LinqIntegrationTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }

        [Test]
        public void CanQueryEntitySnapshot()
        {
            using (var session = db.CreateSession())
            {
                var entity = new Entity { Id = 42 };
                session.Save(entity);
                session.Flush();
                clock.Advance(TimeSpan.FromSeconds(1));

                Assert.IsNotNull(session.At(clock.Now).GetModel<EntityAuditHistory, int>().Get(42));
            }
        }

        [Test]
        public void CanQueryListSnapshot()
        {
            using (var session = db.CreateSession())
            {
                var entity = new Entity
                {
                    Id = 42,
                    List = { "A", "B", "C" }
                };
                session.Save(entity);
                session.Flush();
                clock.Advance(TimeSpan.FromSeconds(1));

                var model = session.At(clock.Now).GetModel<EntityAuditHistory, int>();

                var entitySnapshot = model.Get(42);

                Assert.IsNotEmpty(model.QueryListOf<string>().Using<EntityListValuesAuditHistory>().Fetch(entitySnapshot));
            }
        }

        [Test]
        public void CanQueryMapSnapshot()
        {
            using (var session = db.CreateSession())
            {
                var entity = new Entity
                {
                    Id = 42,
                    Map = {
                        { "A", "One" },
                        { "B", "Two" },
                        { "C", "Three" }
                    }
                };
                session.Save(entity);
                session.Flush();
                clock.Advance(TimeSpan.FromSeconds(1));

                var model = session.At(clock.Now).GetModel<EntityAuditHistory, int>();

                var entitySnapshot = model.Get(42);

                Assert.IsNotEmpty(model.QueryMapOf<string, string>().Using<EntityMapValuesAuditHistory>().Fetch(entitySnapshot));
            }
        }

        [Test]
        public void CanQuerySetSnapshot()
        {
            using (var session = db.CreateSession())
            {
                var entity = new Entity
                {
                    Id = 42,
                    Set = { "A", "B", "C" }
                };
                session.Save(entity);
                session.Flush();
                clock.Advance(TimeSpan.FromSeconds(1));

                var model = session.At(clock.Now).GetModel<EntityAuditHistory, int>();

                var entitySnapshot = model.Get(42);

                Assert.IsNotEmpty(model.QuerySetOf<string>().Using<EntitySetValuesAuditHistory>().Fetch(entitySnapshot));
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<Entity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.List(i => i.List,
                    c => { c.Table("EntityListValues"); },
                    r => r.Element()
                    );
                e.Map(i => i.Map,
                    c => { c.Table("EntityMapValues"); },
                    r => r.Element()
                    );
                e.Set(i => i.Set,
                   c => { c.Table("EntitySetValues"); },
                   r => r.Element()
                   );
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.Id);
                e.Property(i => i.VersionId);
                e.Property(i => i.PreviousVersionId);
                e.Property(i => i.AuditDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.AuditedOperation, p => p.Type<AuditedOperationEnumType>());
                e.Mutable(false);
            });
            mapper.Class<EntityListValuesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.StartDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.EndDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.Key);
                e.Property(i => i.OwnerId);
                e.Property(i => i.Value);
                e.Mutable(false);
            });
            mapper.Class<EntityMapValuesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.StartDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.EndDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.Key);
                e.Property(i => i.OwnerId);
                e.Property(i => i.Value);
                e.Mutable(false);
            });
            mapper.Class<EntitySetValuesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.StartDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.EndDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.OwnerId);
                e.Property(i => i.Value);
                e.Mutable(false);
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            var auditEntryFactory = new AutoAuditEntryFactory(x =>
            {
                x.CreateMap<Entity, EntityAuditHistory>().IgnoreHistoryMetadata();
            });
            new AuditConfigurer(auditEntryFactory, new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}
