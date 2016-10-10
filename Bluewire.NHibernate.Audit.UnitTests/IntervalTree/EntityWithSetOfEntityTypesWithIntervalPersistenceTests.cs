using System;
using System.Linq;
using Bluewire.Common.Time;
using Bluewire.IntervalTree;
using Bluewire.NHibernate.Audit.Support;
using Bluewire.NHibernate.Audit.UnitTests.OneToMany.Entity;
using Bluewire.NHibernate.Audit.UnitTests.Util;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.IntervalTree
{
    [TestFixture]
    public class EntityWithSetOfEntityTypesWithIntervalPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();
        private RitExpectations expectations = new RitExpectations(new PerMinuteSnapshotIntervalTree32());

        public EntityWithSetOfEntityTypesWithIntervalPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }

        [Test]
        public void SavingCollectionGeneratesValidRitEntries()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfEntityTypesWithInterval
                {
                    Id = 42,
                    Entities =
                    {
                        new OneToManyEntity { Id = 2, Value = "2" },
                        new OneToManyEntity { Id = 7, Value = "6" },
                    }
                };
                session.Save(entity);
                session.Flush();
                session.Clear();

                var auditedEntity = session.Query<EntityWithSetOfEntityTypesWithIntervalAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithSetOfEntityTypesWithIntervalEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                foreach(var audited in auditedCollection)
                {
                    expectations.VerifyCurrentRitEntry(audited.RitMinutes, clock.Now);
                }
            }
        }

        [Test]
        public void AddingElementToCollectionGeneratesValidRitEntry()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfEntityTypesWithInterval
                {
                    Id = 42,
                    Entities =
                    {
                        new OneToManyEntity { Id = 2, Value = "2" }
                    }
                };
                session.Save(entity);
                session.Flush();

                clock.Advance(TimeSpan.FromMinutes(5));

                entity.Entities.Add(new OneToManyEntity { Id = 7, Value = "8" });
                session.Flush();
                session.Clear();

                var auditedEntities = session.Query<EntityWithSetOfEntityTypesWithIntervalAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithSetOfEntityTypesWithIntervalEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                expectations.VerifyCurrentRitEntry(auditedCollection[1].RitMinutes, clock.Now);
            }
        }

        [Test]
        public void RemovingElementFromCollection_UpdatesLatestEntrysUpperBound_ButLeavesNodesStale()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfEntityTypesWithInterval
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

                clock.Advance(TimeSpan.FromMinutes(5));

                entity.Entities.Remove(entity.Entities.Single(e => e.Id == 2));

                session.Flush();
                session.Clear();

                var auditedCollection = session.Query<EntityWithSetOfEntityTypesWithIntervalEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var auditedCollectionEntries = session.Query<OneToManyEntityAuditHistory>().ToList();
                Assert.That(auditedCollectionEntries.Count, Is.EqualTo(2));

                var removed = auditedCollection.Single(e => e.Value == 2);
                Assert.IsNotNull(removed.EndDatestamp);

                expectations.VerifyPreviousRitEntry(removed.RitMinutes, clock.Now);
            }
        }

        [Test]
        public void DeletingCollectionOwner_UpdatesLatestEntriesUpperBounds_ButLeavesNodesStale()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithSetOfEntityTypesWithInterval
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

                clock.Advance(TimeSpan.FromMinutes(5));

                session.Delete(entity);
                session.Flush();
                session.Clear();

                var auditedCollection = session.Query<EntityWithSetOfEntityTypesWithIntervalEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                expectations.VerifyPreviousRitEntry(auditedCollection[0].RitMinutes, clock.Now);
                expectations.VerifyPreviousRitEntry(auditedCollection[1].RitMinutes, clock.Now);
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithSetOfEntityTypesWithInterval>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Set(i => i.Entities, c =>
                {
                    c.Cascade(Cascade.All);
                }, m => m.OneToMany());
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<OneToManyEntity>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.Property(i => i.Value);
                e.Version(i => i.VersionId, v => { });
            });
            mapper.Class<EntityWithSetOfEntityTypesWithIntervalAuditHistory>(e =>
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
            mapper.Class<EntityWithSetOfEntityTypesWithIntervalEntitiesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.StartDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.EndDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.OwnerId);
                e.Property(i => i.Value);
                e.Component(i => i.RitMinutes, r => {
                    r.Property(i => i.Lower);
                    r.Property(i => i.Node);
                    r.Property(i => i.Upper);
                    r.Property(i => i.Status, p => p.Type<RitStatusEnumType>());
                });
            });
            cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

            new AuditConfigurer(new DynamicAuditEntryFactory(), new ClockAuditDatestampProvider(clock)).IntegrateWithNHibernate(cfg);
        }
    }
}