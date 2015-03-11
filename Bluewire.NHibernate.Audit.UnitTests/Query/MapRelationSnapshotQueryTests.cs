using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bluewire.NHibernate.Audit.Query;
using NUnit.Framework;

namespace Bluewire.NHibernate.Audit.UnitTests.Query
{
    [TestFixture]
    public class MapRelationSnapshotQueryTests
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
            public void MemberDoesNotExistInMapPriorToAdd()
            {
                var beforeAdd = history.GetNow();
                history.AdvanceTime();
                history.AuditAddWithKey<ComponentMapMember, string, string>(new ComponentMapMember { OwnerId = 1, Key = "A", Value = "One" });

                var snapshot = history.At(beforeAdd).GetModel<EntityAudit, int>()
                    .QueryMapOf<string, string>().Using<ComponentMapMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.IsEmpty(snapshot);
            }

            [Test]
            public void MemberExistsInMapAfterAdd()
            {
                history.AuditAddWithKey<ComponentMapMember, string, string>(new ComponentMapMember { OwnerId = 1, Key = "A", Value = "One" });
                history.AdvanceTime();
                var afterAdd = history.GetNow();

                var snapshot = history.At(afterAdd).GetModel<EntityAudit, int>()
                    .QueryMapOf<string, string>().Using<ComponentMapMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.That(snapshot.ContainsKey("A"));
                Assert.That(snapshot["A"], Is.EqualTo("One"));
            }

            [Test]
            public void MemberDoesNotExistInMapAfterRemove()
            {
                history.AuditAddWithKey<ComponentMapMember, string, string>(new ComponentMapMember { OwnerId = 1, Key = "A", Value = "One" });
                history.AdvanceTime();
                history.AuditRemoveWithKey<ComponentMapMember, string, string>(new ComponentMapMember { OwnerId = 1, Key = "A", Value = "One" });
                history.AdvanceTime();
                var afterRemove = history.GetNow();

                var snapshot = history.At(afterRemove).GetModel<EntityAudit, int>()
                    .QueryMapOf<string, string>().Using<ComponentMapMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.IsEmpty(snapshot);
            }

            class ComponentMapMember : KeyedRelationAuditHistoryEntry<int, string, string>
            {
                public new string Key { get { return base.Key; } set { base.Key = value; } }
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
                history.Audit(new MapMemberAudit { Id = 1, AuditedOperation = AuditedOperation.Add, Value = "One" });
                history.AdvanceTime();
            }

            [Test]
            public void MemberDoesNotExistInMapPriorToAdd()
            {
                var beforeAdd = history.GetNow();
                history.AdvanceTime();
                history.AuditAddWithKey<EntityMapMember, string, int>(new EntityMapMember { OwnerId = 1, Key = "A", Value = 1 });

                var snapshot = history.At(beforeAdd).GetModel<EntityAudit, int>()
                    .QueryMapOf<string, MapMemberAudit, int>().Using<EntityMapMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.IsEmpty(snapshot);
            }

            [Test]
            public void MemberExistsInMapAfterAdd()
            {
                history.AuditAddWithKey<EntityMapMember, string, int>(new EntityMapMember { OwnerId = 1, Key = "A", Value = 1 });
                history.AdvanceTime();
                var afterAdd = history.GetNow();

                var snapshot = history.At(afterAdd).GetModel<EntityAudit, int>()
                    .QueryMapOf<string, MapMemberAudit, int>().Using<EntityMapMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.That(snapshot.ContainsKey("A"));
                Assert.That(snapshot["A"].Value, Is.EqualTo("One"));
            }

            [Test]
            public void MemberDoesNotExistInMapAfterRemove()
            {
                history.AuditAddWithKey<EntityMapMember, string, int>(new EntityMapMember { OwnerId = 1, Key = "A", Value = 1 });
                history.AdvanceTime();
                history.AuditRemoveWithKey<EntityMapMember, string, int>(new EntityMapMember { OwnerId = 1, Key = "A", Value = 1 });
                history.AdvanceTime();
                var afterRemove = history.GetNow();

                var snapshot = history.At(afterRemove).GetModel<EntityAudit, int>()
                    .QueryMapOf<string, MapMemberAudit, int>().Using<EntityMapMember>().Fetch(new EntityAudit { Id = 1 });

                Assert.IsEmpty(snapshot);
            }
            
            class EntityMapMember : KeyedRelationAuditHistoryEntry<int, string, int>
            {
                public new string Key { get { return base.Key; } set { base.Key = value; } }
                public new int OwnerId { get { return base.OwnerId; } set { base.OwnerId = value; } }
                public new int Value { get { return base.Value; } set { base.Value = value; } }
            }
            
            class MapMemberAudit : EntityAuditHistoryBase<int, Guid>
            {
                public string Value { get; set; }
            }
        }

        class EntityAudit : EntityAuditHistoryBase<int, Guid>
        {
        }
    }

}
