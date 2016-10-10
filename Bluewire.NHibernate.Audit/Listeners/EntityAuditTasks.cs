using System;
using Bluewire.NHibernate.Audit.Meta;
using Bluewire.NHibernate.Audit.Model;
using NHibernate.Event;

namespace Bluewire.NHibernate.Audit.Listeners
{
    public class EntityAuditTasks
    {
        private readonly Rit32Tasks ritTasks;

        public EntityAuditTasks(AuditModel model)
        {
            this.ritTasks = new Rit32Tasks(model);
        }

        public void ApplyRitForAdd(IEventSource session, IEntityAuditHistory newEntry, IAuditableEntityModel entityModel, DateTimeOffset operationDatestamp)
        {
            if (entityModel.RitProperty == null) return;
            ritTasks.AssignRitEntry32ForNewRecord(newEntry, entityModel.RitProperty, operationDatestamp);
        }

        public void ApplyRitForUpdate(IEventSource session, IEntityAuditHistory newEntry, IAuditableEntityModel entityModel, DateTimeOffset operationDatestamp)
        {
            if (entityModel.RitProperty == null) return;
            ritTasks.AssignRitEntry32ForNewRecord(newEntry, entityModel.RitProperty, operationDatestamp);
            ritTasks.UpdateRitEntry32ForPreviousEntityRecord(session, newEntry, entityModel.RitProperty, operationDatestamp);
        }

        public void ApplyRitForDelete(IEventSource session, IEntityAuditHistory newEntry, IAuditableEntityModel entityModel, DateTimeOffset operationDatestamp)
        {
            if (entityModel.RitProperty == null) return;
            ritTasks.AssignRitEntry32ForNewRecord(newEntry, entityModel.RitProperty, operationDatestamp);
            ritTasks.UpdateRitEntry32ForPreviousEntityRecord(session, newEntry, entityModel.RitProperty, operationDatestamp);
        }
    }
}
