using System;
using System.Linq;
using Bluewire.NHibernate.Audit.Query;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Query
{
    [TestFixture]
    public class EntitySnapshotQueryTests
    {
        [Test]
        public void EntityDoesNotExistPriorToFirstSave()
        {
            var history = new MockAuditHistory();

            var beforeSave = history.GetNow();
            history.AdvanceTime();
            history.Audit(new EntityAudit { Id = 1, Value = "One", AuditedOperation = AuditedOperation.Add });

            var snapshot = history.At(beforeSave).GetModel<EntityAudit, int>().Get(1);

            Assert.IsNull(snapshot);
        }

        [Test]
        public void EntityExistsAfterFirstSave()
        {
            var history = new MockAuditHistory();
            history.Audit(new EntityAudit { Id = 1, Value = "One", AuditedOperation = AuditedOperation.Add });
            history.AdvanceTime();
            var afterSave = history.GetNow();

            var snapshot = history.At(afterSave).GetModel<EntityAudit, int>().Get(1);

            Assert.IsNotNull(snapshot);
        }

        [Test]
        public void SnapshotReturnsOnlyImmediatePriorRecord()
        {
            var history = new MockAuditHistory();

            history.Audit(new EntityAudit { Id = 1, Value = "One", AuditedOperation = AuditedOperation.Add });
            history.AdvanceTime();
            history.Audit(new EntityAudit { Id = 1, Value = "Two", AuditedOperation = AuditedOperation.Update });
            history.AdvanceTime();
            var afterUpdate = history.GetNow();
            history.AdvanceTime();
            history.Audit(new EntityAudit { Id = 1, Value = "Three", AuditedOperation = AuditedOperation.Update });


            var snapshot = history.At(afterUpdate).GetModel<EntityAudit, int>().Query.Where(e => e.Id == 1).ToList();

            Assert.That(snapshot, Has.Count.EqualTo(1));
            Assert.That(snapshot.Single().Value, Is.EqualTo("Two"));
        }


        class EntityAudit : EntityAuditHistoryBase<int, Guid>
        {
            public string Value { get; set; }
        }
    }
}
