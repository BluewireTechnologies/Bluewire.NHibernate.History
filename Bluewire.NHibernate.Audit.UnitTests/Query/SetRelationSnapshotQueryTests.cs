using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bluewire.NHibernate.Audit.Query;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Query
{
    [TestFixture]
    public class SetRelationSnapshotQueryTests
    {
        public class ComponentMembers
        {
            private MockAuditHistory history;

            [SetUp]
            public void SetUp()
            {
                history = new MockAuditHistory();
                history.Audit(new EntityAudit { Id = 1, AuditedOperation = AuditedOperation.Add });
                history.AdvanceTime();
            }

            [Test]
            public void MemberDoesNotExistInSetPriorToAdd()
            {
                var beforeAdd = history.GetNow();
                history.AdvanceTime();
                history.AuditAddToSet<ComponentSetMember, string>(new ComponentSetMember { OwnerId = 1, Value = "One" });

                var snapshot = history.At(beforeAdd).GetModel<EntityAudit, int>()
                    .QuerySetOf<string>().Using<ComponentSetMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.IsEmpty(snapshot);
            }

            [Test]
            public void MemberExistsInSetAfterAdd()
            {
                history.AuditAddToSet<ComponentSetMember, string>(new ComponentSetMember { OwnerId = 1, Value = "One" });
                history.AdvanceTime();
                var afterAdd = history.GetNow();

                var snapshot = history.At(afterAdd).GetModel<EntityAudit, int>()
                    .QuerySetOf<string>().Using<ComponentSetMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.That(snapshot, Has.Member("One"));
            }

            [Test]
            public void MemberDoesNotExistInSetAfterRemove()
            {
                history.AuditAddToSet<ComponentSetMember, string>(new ComponentSetMember { OwnerId = 1, Value = "One" });
                history.AdvanceTime();
                history.AuditRemoveFromSet<ComponentSetMember, string>(new ComponentSetMember { OwnerId = 1, Value = "One" });
                history.AdvanceTime();
                var afterRemove = history.GetNow();

                var snapshot = history.At(afterRemove).GetModel<EntityAudit, int>()
                    .QuerySetOf<string>().Using<ComponentSetMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.IsEmpty(snapshot);
            }

            class ComponentSetMember : SetRelationAuditHistoryEntry<int, string>
            {
                public new int OwnerId { get { return base.OwnerId; } set { base.OwnerId = value; } }
                public new string Value { get { return base.Value; } set { base.Value = value; } }
            }
        }

        public class EntityMembers
        {
            private MockAuditHistory history;

            [SetUp]
            public void SetUp()
            {
                history = new MockAuditHistory();
                history.Audit(new EntityAudit { Id = 1, AuditedOperation = AuditedOperation.Add });
                history.Audit(new SetMemberAudit { Id = 1, AuditedOperation = AuditedOperation.Add, Value = "One" });
                history.AdvanceTime();
            }

            [Test]
            public void MemberDoesNotExistInSetPriorToAdd()
            {
                var beforeAdd = history.GetNow();
                history.AdvanceTime();
                history.AuditAddToSet<EntitySetMember, int>(new EntitySetMember { OwnerId = 1, Value = 1 });

                var snapshot = history.At(beforeAdd).GetModel<EntityAudit, int>()
                    .QuerySetOf<SetMemberAudit, int>().Using<EntitySetMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.IsEmpty(snapshot);
            }

            [Test]
            public void MemberExistsInSetAfterAdd()
            {
                history.AuditAddToSet<EntitySetMember, int>(new EntitySetMember { OwnerId = 1, Value = 1 });
                history.AdvanceTime();
                var afterAdd = history.GetNow();

                var snapshot = history.At(afterAdd).GetModel<EntityAudit, int>()
                    .QuerySetOf<SetMemberAudit, int>().Using<EntitySetMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.That(snapshot, Has.Count.EqualTo(1));
                Assert.That(snapshot.Single().Value, Is.EqualTo("One"));
            }

            [Test]
            public void MemberDoesNotExistInSetAfterRemove()
            {
                history.AuditAddToSet<EntitySetMember, int>(new EntitySetMember { OwnerId = 1, Value = 1 });
                history.AdvanceTime();
                history.AuditRemoveFromSet<EntitySetMember, int>(new EntitySetMember { OwnerId = 1, Value = 1 });
                history.AdvanceTime();
                var afterRemove = history.GetNow();

                var snapshot = history.At(afterRemove).GetModel<EntityAudit, int>()
                    .QuerySetOf<SetMemberAudit, int>().Using<EntitySetMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.IsEmpty(snapshot);
            }

            class EntitySetMember : SetRelationAuditHistoryEntry<int, int>
            {
                public new int OwnerId { get { return base.OwnerId; } set { base.OwnerId = value; } }
                public new int Value { get { return base.Value; } set { base.Value = value; } }
            }

            class SetMemberAudit : EntityAuditHistoryBase<int, Guid>
            {
                public string Value { get; set; }
            }
        }

        class EntityAudit : EntityAuditHistoryBase<int, Guid>
        {
        }
    }

}
