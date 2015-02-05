using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        class SetAuditEntryType : ISetRelationAuditHistory
        {
            public long AuditId { get; private set; }
            public DateTimeOffset StartDatestamp { get; set; }
            public DateTimeOffset? EndDatestamp { get; set; }
            public object OwnerId { get; set; }
            public object Value { get; set; }
        }
    }
}
