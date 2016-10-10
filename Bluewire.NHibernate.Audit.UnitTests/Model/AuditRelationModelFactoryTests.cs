using System;
using Bluewire.IntervalTree;
using Bluewire.NHibernate.Audit.Attributes;
using Bluewire.NHibernate.Audit.Meta;
using Bluewire.NHibernate.Audit.Model;
using NHibernate;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Model
{
    [TestFixture]
    public class AuditRelationModelFactoryTests
    {
        [Test]
        public void ComponentTypeOverrideWhichConflictsWithKnownTypeIsInvalid()
        {
            var attribute = new AuditableRelationAttribute(typeof(KnownAuditEntryType)) { AuditValueType = typeof(int[]) };
            var inferred = new InferredRelationAuditInfo("Test", NHibernateUtil.Int32, NHibernateUtil.Int32);

            Assert.Throws<AuditConfigurationException>(() => AuditRelationModelFactory.CreateComponentRelationModel(typeof(object), attribute, inferred));
        }

        [Test]
        public void ComponentTypeOverrideWhichMatchesKnownTypeIsValid()
        {
            var attribute = new AuditableRelationAttribute(typeof(KnownAuditEntryType)) { AuditValueType = typeof(AuditComponentBase) };
            var inferred = new InferredRelationAuditInfo("Test", NHibernateUtil.Int32, NHibernateUtil.Int32);

            var model = AuditRelationModelFactory.CreateComponentRelationModel(typeof(object), attribute, inferred);

            Assert.AreEqual(typeof(AuditComponentBase), model.AuditValueType);
        }

        [Test]
        public void ComponentTypeOverrideWhichIsAssignableToKnownTypeOverridesKnownType()
        {
            var attribute = new AuditableRelationAttribute(typeof(KnownAuditEntryType)) { AuditValueType = typeof(AuditComponentDerived) };
            var inferred = new InferredRelationAuditInfo("Test", NHibernateUtil.Int32, NHibernateUtil.Int32);

            var model = AuditRelationModelFactory.CreateComponentRelationModel(typeof(object), attribute, inferred);

            Assert.AreEqual(typeof(AuditComponentDerived), model.AuditValueType);
        }

        [Test]
        public void ComponentTypeOverrideIsAcceptedIfAuditTypeIsNotKnown()
        {
            var attribute = new AuditableRelationAttribute(typeof(KeyedAuditEntryType)) { AuditValueType = typeof(AuditComponentDerived) };
            var inferred = new InferredRelationAuditInfo("Test", NHibernateUtil.Int32, NHibernateUtil.Int32);

            var model = AuditRelationModelFactory.CreateComponentRelationModel(typeof(object), attribute, inferred);

            Assert.AreEqual(typeof(AuditComponentDerived), model.AuditValueType);
        }

        [Test]
        public void InferredComponentTypeIsUsedIfAuditTypeIsKnownAndNoOverrideSpecified()
        {
            var attribute = new AuditableRelationAttribute(typeof(KnownAuditEntryType));
            var inferred = new InferredRelationAuditInfo("Test", NHibernateUtil.Int32, NHibernateUtil.Int32)
            {
                ElementType = NHibernateUtil.Entity(typeof(AuditComponentDerived))
            };

            var model = AuditRelationModelFactory.CreateComponentRelationModel(typeof(object), attribute, inferred);

            Assert.AreEqual(typeof(AuditComponentBase), model.AuditValueType);
        }

        [Test]
        public void SourceElementTypeIsUsedIfAuditTypeIsNotKnownAndNoOverrideSpecified()
        {
            var attribute = new AuditableRelationAttribute(typeof(KeyedAuditEntryType));
            var inferred = new InferredRelationAuditInfo("Test", NHibernateUtil.Int32, NHibernateUtil.Int32)
            {
                ElementType = NHibernateUtil.Entity(typeof(AuditComponentDerived))
            };

            var model = AuditRelationModelFactory.CreateComponentRelationModel(typeof(object), attribute, inferred);

            Assert.AreEqual(typeof(AuditComponentDerived), model.AuditValueType);
        }

        [Test]
        public void SetAuditTypeCannotBeUsedForKeyedCollection()
        {
            var attribute = new AuditableRelationAttribute(typeof(SetAuditEntryType)) { AuditValueType = typeof(int[]) };
            var inferred = new InferredRelationAuditInfo("Test", NHibernateUtil.Int32, NHibernateUtil.Int32);

            Assert.Throws<AuditConfigurationException>(() => AuditRelationModelFactory.CreateComponentRelationModel(typeof(object), attribute, inferred));
        }

        [Test]
        public void KeyedAuditTypeCannotBeUsedForSetCollection()
        {
            var attribute = new AuditableRelationAttribute(typeof(KeyedAuditEntryType)) { AuditValueType = typeof(int[]) };
            var inferred = new InferredRelationAuditInfo("Test", NHibernateUtil.Int32);

            Assert.Throws<AuditConfigurationException>(() => AuditRelationModelFactory.CreateComponentRelationModel(typeof(object), attribute, inferred));
        }

        [Test]
        public void CanModelASetAuditTypeWithRitProperty()
        {
            var attribute = new AuditableRelationAttribute(typeof(SetAuditEntryTypeWithInterval));
            var inferred = new InferredRelationAuditInfo("Test", NHibernateUtil.Int32)
            {
                ElementType = NHibernateUtil.Entity(typeof(AuditComponentDerived))
            };

            var model = AuditRelationModelFactory.CreateComponentRelationModel(typeof(object), attribute, inferred);
            
            Assert.That(model.RitProperty, Is.Not.Null);
            Assert.That(model.RitProperty.Property.Name, Is.EqualTo(nameof(SetAuditEntryTypeWithInterval.RitMinutes)));
            Assert.That(model.RitProperty.IntervalTree, Is.InstanceOf<PerMinuteSnapshotIntervalTree32>());
        }

        [Test]
        public void CanModelAKeyedAuditTypeWithRitProperty()
        {
            var attribute = new AuditableRelationAttribute(typeof(KeyedAuditEntryTypeWithInterval)) { AuditValueType = typeof(int[]) };
            var inferred = new InferredRelationAuditInfo("Test", NHibernateUtil.Int32, NHibernateUtil.Int32);

            var model = AuditRelationModelFactory.CreateComponentRelationModel(typeof(object), attribute, inferred);
            
            Assert.That(model.RitProperty, Is.Not.Null);
            Assert.That(model.RitProperty.Property.Name, Is.EqualTo(nameof(KeyedAuditEntryTypeWithInterval.RitMinutes)));
            Assert.That(model.RitProperty.IntervalTree, Is.InstanceOf<PerMinuteSnapshotIntervalTree32>());
        }

        class AuditComponentBase
        {
        }

        class AuditComponentDerived : AuditComponentBase
        {
        }


        class KnownAuditEntryType : KeyedRelationAuditHistoryEntry<int, int, AuditComponentBase>
        {
        }

        class KeyedAuditEntryType : IKeyedRelationAuditHistory
        {
            public long AuditId { get; private set; }
            public DateTimeOffset StartDatestamp { get; set; }
            public DateTimeOffset? EndDatestamp { get; set; }
            public object OwnerId { get; set; }
            public object Value { get; set; }
            public object Key { get; set; }
        }

        class KeyedAuditEntryTypeWithInterval : IKeyedRelationAuditHistory
        {
            public long AuditId { get; private set; }
            public DateTimeOffset StartDatestamp { get; set; }
            public DateTimeOffset? EndDatestamp { get; set; }
            public object OwnerId { get; set; }
            public object Value { get; set; }
            public object Key { get; set; }

            [AuditInterval(typeof(PerMinuteSnapshotIntervalTree32))]
            public RitEntry32 RitMinutes { get; set; }
        }

        class SetAuditEntryType : ISetRelationAuditHistory
        {
            public long AuditId { get; private set; }
            public DateTimeOffset StartDatestamp { get; set; }
            public DateTimeOffset? EndDatestamp { get; set; }
            public object OwnerId { get; set; }
            public object Value { get; set; }
        }

        class SetAuditEntryTypeWithInterval : ISetRelationAuditHistory
        {
            public long AuditId { get; private set; }
            public DateTimeOffset StartDatestamp { get; set; }
            public DateTimeOffset? EndDatestamp { get; set; }
            public object OwnerId { get; set; }
            public object Value { get; set; }

            [AuditInterval(typeof(PerMinuteSnapshotIntervalTree32))]
            public RitEntry32 RitMinutes { get; set; }
        }
    }
}
