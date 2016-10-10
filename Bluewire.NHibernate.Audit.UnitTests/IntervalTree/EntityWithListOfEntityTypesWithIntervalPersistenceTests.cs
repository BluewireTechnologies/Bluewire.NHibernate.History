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
    public class EntityWithListOfEntityTypesWithIntervalPersistenceTests
    {
        private TemporaryDatabase db;
        private MockClock clock = new MockClock();
        private RitExpectations expectations = new RitExpectations(new PerMinuteSnapshotIntervalTree32());

        public EntityWithListOfEntityTypesWithIntervalPersistenceTests()
        {
            db = TemporaryDatabase.Configure(Configure);
        }

        [Test]
        public void SavingCollectionGeneratesValidRitEntries()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfEntityTypesWithInterval
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

                var auditedEntity = session.Query<EntityWithListOfEntityTypesWithIntervalAuditHistory>().Single(h => h.Id == 42);
                Assert.AreEqual(42, auditedEntity.Id);

                var auditedCollection = session.Query<EntityWithListOfEntityTypesWithIntervalEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
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
                var entity = new EntityWithListOfEntityTypesWithInterval
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

                var auditedEntities = session.Query<EntityWithListOfEntityTypesWithIntervalAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithListOfEntityTypesWithIntervalEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                expectations.VerifyCurrentRitEntry(auditedCollection[1].RitMinutes, clock.Now);
            }
        }

        [Test]
        public void ReorderingCollectionGeneratesValidRitEntries_AndUpdatesPreviousEntriesUpperBounds_ButLeavesNodesStale()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfEntityTypesWithInterval
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

                var temp = entity.Entities[0];
                entity.Entities[0] = entity.Entities[1];
                entity.Entities[1] = temp;
                session.Flush();
                session.Clear();

                var auditedEntities = session.Query<EntityWithListOfEntityTypesWithIntervalAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithListOfEntityTypesWithIntervalEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(4));

                var auditedCollectionEntries = session.Query<OneToManyEntityAuditHistory>().ToList();
                Assert.That(auditedCollectionEntries.Count, Is.EqualTo(2));

                expectations.VerifyPreviousRitEntry(auditedCollection[0].RitMinutes, clock.Now);
                expectations.VerifyPreviousRitEntry(auditedCollection[1].RitMinutes, clock.Now);

                expectations.VerifyCurrentRitEntry(auditedCollection[2].RitMinutes, clock.Now);
                expectations.VerifyCurrentRitEntry(auditedCollection[3].RitMinutes, clock.Now);
            }
        }

        [Test]
        public void RemovingElementFromEndOfCollection_UpdatesLatestEntrysUpperBound_ButLeavesNodesStale()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfEntityTypesWithInterval
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

                entity.Entities.RemoveAt(1);
                session.Flush();
                session.Clear();

                var auditedCollection = session.Query<EntityWithListOfEntityTypesWithIntervalEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                var auditedCollectionEntries = session.Query<OneToManyEntityAuditHistory>().ToList();
                Assert.That(auditedCollectionEntries.Count, Is.EqualTo(2));

                Assert.AreEqual(7, auditedCollection.ElementAt(1).Value);
                Assert.IsNotNull(auditedCollection.ElementAt(1).EndDatestamp);

                expectations.VerifyPreviousRitEntry(auditedCollection[1].RitMinutes, clock.Now);
            }
        }

        [Test]
        public void RemovingElementFromStartOfCollection_UpdatesLatestEntrysUpperBound_ButLeavesNodesStale()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfEntityTypesWithInterval
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

                entity.Entities.RemoveAt(0);
                session.Flush();
                session.Clear();

                var auditedEntities = session.Query<EntityWithListOfEntityTypesWithIntervalAuditHistory>().Where(h => h.Id == 42).ToList();
                Assert.That(auditedEntities.Count, Is.AtLeast(2));

                var auditedCollection = session.Query<EntityWithListOfEntityTypesWithIntervalEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(3));

                var auditedCollectionEntries = session.Query<OneToManyEntityAuditHistory>().ToList();
                Assert.That(auditedCollectionEntries.Count, Is.EqualTo(2));

                var removedIndex0 = auditedCollection.Single(o => o.Key == 0 && o.EndDatestamp.HasValue);
                var remainingIndex0 = auditedCollection.Single(o => o.Key == 0 && !o.EndDatestamp.HasValue);

                Assert.AreEqual(7, remainingIndex0.Value);

                expectations.VerifyPreviousRitEntry(removedIndex0.RitMinutes, clock.Now);
            }
        }
        
        [Test]
        public void DeletingCollectionOwner_UpdatesLatestEntriesUpperBounds_ButLeavesNodesStale()
        {
            using (var session = db.CreateSession())
            {
                var entity = new EntityWithListOfEntityTypesWithInterval
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

                var auditedCollection = session.Query<EntityWithListOfEntityTypesWithIntervalEntitiesAuditHistory>().Where(h => h.OwnerId == 42).ToList();
                Assert.That(auditedCollection.Count, Is.EqualTo(2));

                expectations.VerifyPreviousRitEntry(auditedCollection[0].RitMinutes, clock.Now);
                expectations.VerifyPreviousRitEntry(auditedCollection[1].RitMinutes, clock.Now);
            }
        }

        private void Configure(Configuration cfg)
        {
            var mapper = new ModelMapper();
            mapper.Class<EntityWithListOfEntityTypesWithInterval>(e =>
            {
                e.Id(i => i.Id, i => i.Generator(new AssignedGeneratorDef()));
                e.List(i => i.Entities, c =>
                {
                    c.Index(i => i.Column("idx"));
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
            mapper.Class<EntityWithListOfEntityTypesWithIntervalAuditHistory>(e =>
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
            mapper.Class<EntityWithListOfEntityTypesWithIntervalEntitiesAuditHistory>(e =>
            {
                e.Id(i => i.AuditId, i => i.Generator(new HighLowGeneratorDef()));
                e.Property(i => i.StartDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.EndDatestamp, p => p.Type<DateTimeOffsetAsIntegerUserType>());
                e.Property(i => i.Key);
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