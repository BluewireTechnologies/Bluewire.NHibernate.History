using System;
using Bluewire.NHibernate.Audit.Query;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Query
{
    [TestFixture]
    public class ListRelationSnapshotQueryTests
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
            public void MemberDoesNotExistInListPriorToAdd()
            {
                var beforeAdd = history.GetNow();
                history.AdvanceTime();
                history.AuditAddWithIndex<ComponentListMember, string>(new ComponentListMember { OwnerId = 1, Index = 2, Value = "One" });

                var snapshot = history.At(beforeAdd).GetModel<EntityAudit, int>()
                    .QueryListOf<string>().Using<ComponentListMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.IsEmpty(snapshot);
            }

            [Test]
            public void MemberExistsInListAfterAdd()
            {
                history.AuditAddWithKey<ComponentListMember, int, string>(new ComponentListMember { OwnerId = 1, Index = 2, Value = "One" });
                history.AdvanceTime();
                var afterAdd = history.GetNow();

                var snapshot = history.At(afterAdd).GetModel<EntityAudit, int>()
                    .QueryListOf<string>().Using<ComponentListMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.That(snapshot, Has.Count.EqualTo(3));
                Assert.That(snapshot[2], Is.EqualTo("One"));
            }

            [Test]
            public void MemberDoesNotExistInListAfterRemove()
            {
                history.AuditAddWithKey<ComponentListMember, int, string>(new ComponentListMember { OwnerId = 1, Index = 2, Value = "One" });
                history.AdvanceTime();
                history.AuditRemoveWithKey<ComponentListMember, int, string>(new ComponentListMember { OwnerId = 1, Index = 2, Value = "One" });
                history.AdvanceTime();
                var afterRemove = history.GetNow();

                var snapshot = history.At(afterRemove).GetModel<EntityAudit, int>()
                    .QueryListOf<string>().Using<ComponentListMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.IsEmpty(snapshot);
            }

            class ComponentListMember : KeyedRelationAuditHistoryEntry<int, int, string>
            {
                public int Index { get { return base.Key; } set { base.Key = value; } }
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
                history.Audit(new ListMemberAudit { Id = 1, AuditedOperation = AuditedOperation.Add, Value = "One" });
                history.AdvanceTime();
            }

            [Test]
            public void MemberDoesNotExistInListPriorToAdd()
            {
                var beforeAdd = history.GetNow();
                history.AdvanceTime();
                history.AuditAddWithIndex<EntityListMember, int>(new EntityListMember { OwnerId = 1, Index = 2, Value = 1 });

                var snapshot = history.At(beforeAdd).GetModel<EntityAudit, int>()
                    .QueryListOf<ListMemberAudit, int>().Using<EntityListMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.IsEmpty(snapshot);
            }

            [Test]
            public void MemberExistsInListAfterAdd()
            {
                history.AuditAddWithIndex<EntityListMember, int>(new EntityListMember { OwnerId = 1, Index = 2, Value = 1 });
                history.AdvanceTime();
                var afterAdd = history.GetNow();

                var snapshot = history.At(afterAdd).GetModel<EntityAudit, int>()
                    .QueryListOf<ListMemberAudit, int>().Using<EntityListMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.That(snapshot, Has.Count.EqualTo(3));
                Assert.That(snapshot[2].Value, Is.EqualTo("One"));
            }

            [Test]
            public void MemberDoesNotExistInListAfterRemove()
            {
                history.AuditAddWithIndex<EntityListMember, int>(new EntityListMember { OwnerId = 1, Index = 2, Value = 1 });
                history.AdvanceTime();
                history.AuditRemoveWithIndex<EntityListMember, int>(new EntityListMember { OwnerId = 1, Index = 2, Value = 1 });
                history.AdvanceTime();
                var afterRemove = history.GetNow();

                var snapshot = history.At(afterRemove).GetModel<EntityAudit, int>()
                    .QueryListOf<ListMemberAudit, int>().Using<EntityListMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.IsEmpty(snapshot);
            }

            class EntityListMember : KeyedRelationAuditHistoryEntry<int, int, int>
            {
                public int Index { get { return base.Key; } set { base.Key = value; } }
                public new int OwnerId { get { return base.OwnerId; } set { base.OwnerId = value; } }
                public new int Value { get { return base.Value; } set { base.Value = value; } }
            }

            class ListMemberAudit : EntityAuditHistoryBase<int, Guid>
            {
                public string Value { get; set; }
            }
        }

        class EntityAudit : EntityAuditHistoryBase<int, Guid>
        {
        }
    }

}
