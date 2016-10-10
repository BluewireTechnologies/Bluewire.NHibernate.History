﻿using System;
using Bluewire.IntervalTree;
using Bluewire.NHibernate.Audit.Attributes;
using Bluewire.NHibernate.Audit.Meta;
using Bluewire.NHibernate.Audit.Model;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Model
{
    [TestFixture]
    public class AuditEntityModelFactoryTests
    {
        [Test, Description("Auditing an audit record makes no sense, but it's easy to put the attribute on the wrong class by accident.")]
        public void CannotAuditAnAuditRecord()
        {
            var attribute = new AuditableEntityAttribute(typeof(EntityHistory));

            Assert.Throws<AuditConfigurationException>(() => new AuditEntityModelFactory().CreateEntityModel(typeof(OtherEntityHistory), attribute));
        }

        [Test, Description("Auditing an audit record with itself makes no sense, but it's easy to put the attribute on the wrong class by accident.")]
        public void CannotAuditAnAuditRecordWithItself()
        {
            var attribute = new AuditableEntityAttribute(typeof(EntityHistory));

            Assert.Throws<AuditConfigurationException>(() => new AuditEntityModelFactory().CreateEntityModel(typeof(EntityHistory), attribute));
        }

        [Test]
        public void AuditRecordMustImplementIEntityAuditHistory()
        {
            var attribute = new AuditableEntityAttribute(typeof(OtherEntity));

            Assert.Throws<AuditConfigurationException>(() => new AuditEntityModelFactory().CreateEntityModel(typeof(Entity), attribute));
        }

        [Test]
        public void CanModelAnAuditRecordWithRitProperty()
        {
            var attribute = new AuditableEntityAttribute(typeof(EntityHistoryWithInterval));

            var model = new AuditEntityModelFactory().CreateEntityModel(typeof(Entity), attribute);

            Assert.That(model.RitProperty, Is.Not.Null);
            Assert.That(model.RitProperty.Property.Name, Is.EqualTo(nameof(EntityHistoryWithInterval.RitMinutes)));
            Assert.That(model.RitProperty.IntervalTree, Is.InstanceOf<PerMinuteSnapshotIntervalTree32>());
        }

        class Entity
        {
        }

        class OtherEntity
        {
        }

        class EntityHistory : IEntityAuditHistory
        {
            public long AuditId { get; private set; }
            public object VersionId { get; set; }
            public object Id { get; private set; }
            public object PreviousVersionId { get; set; }
            public DateTimeOffset AuditDatestamp { get; set; }
            public AuditedOperation AuditedOperation { get; set; }
        }

        class OtherEntityHistory : IEntityAuditHistory
        {
            public long AuditId { get; private set; }
            public object VersionId { get; set; }
            public object Id { get; private set; }
            public object PreviousVersionId { get; set; }
            public DateTimeOffset AuditDatestamp { get; set; }
            public AuditedOperation AuditedOperation { get; set; }
        }

        class EntityHistoryWithInterval : IEntityAuditHistory
        {
            public long AuditId { get; private set; }
            public object VersionId { get; set; }
            public object Id { get; private set; }
            public object PreviousVersionId { get; set; }
            public DateTimeOffset AuditDatestamp { get; set; }
            public AuditedOperation AuditedOperation { get; set; }

            [AuditInterval(typeof(PerMinuteSnapshotIntervalTree32))]
            public RitEntry32 RitMinutes { get; set; }
        }
    }
}
