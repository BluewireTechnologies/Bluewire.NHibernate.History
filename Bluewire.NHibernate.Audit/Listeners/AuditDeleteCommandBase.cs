using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Bluewire.NHibernate.Audit.Support;
using NHibernate.Engine;
using NHibernate.Mapping;
using NHibernate.SqlCommand;

namespace Bluewire.NHibernate.Audit.Listeners
{
    /// <summary>
    /// Generates a command of the form:
    ///     update (audit table) set endDatestamp = ? where (owner key) = ? and (predicates) and endDatestamp is null
    /// 
    /// Implementations are expected to add the necessary predicates for the collection type.
    /// </summary>
    abstract class AuditDeleteCommandBase
    {
        private readonly ISessionFactoryImplementor factory;
        protected readonly Property OwningEntityIdProperty;
        protected readonly Property EndDateProperty;

        protected AuditDeleteCommandBase(ISessionFactoryImplementor factory, PersistentClass auditMapping)
        {
            this.factory = factory;
            SqlUpdateBuilder = new SqlUpdateBuilder(factory.Dialect, factory);
            OwningEntityIdProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "OwnerId");
            EndDateProperty = auditMapping.PropertyClosureIterator.Single(n => n.Name == "EndDatestamp");
            
            SqlUpdateBuilder
                .SetTableName(auditMapping.Table.GetQualifiedName(factory.Dialect, factory.Settings.DefaultCatalogName, factory.Settings.DefaultSchemaName))
                .AddColumns(ColumnNames(factory, EndDateProperty.ColumnIterator), EndDateProperty.Type);

            AddPredicateProperty(OwningEntityIdProperty);
            SqlUpdateBuilder.AddWhereFragment(ColumnNames(factory, EndDateProperty.ColumnIterator).Single() + " is null");
        }

        protected SqlUpdateBuilder SqlUpdateBuilder { get; private set; }

        protected void AddPredicateProperty(Property property)
        {
            SqlUpdateBuilder.AddWhereFragment(ColumnNames(factory, property.ColumnIterator), property.Type, " = ");
        }

        private static string[] ColumnNames(ISessionFactoryImplementor factory, IEnumerable<ISelectable> columnIterator)
        {
            return columnIterator.Select(k => k.GetText(factory.Dialect)).ToArray();
        }

        public SqlCommandInfo Command { get { return SqlUpdateBuilder.ToSqlCommandInfo(); } }

        protected abstract void AddParameters(CommandParameteriser parameters, object deletion);

        public void PopulateCommand(ISessionImplementor session, IDbCommand cmd, object owningEntityId, object deletion, DateTimeOffset deletionDatestamp)
        {
            var parameters = new CommandParameteriser(session, cmd);
            parameters.Set(EndDateProperty, deletionDatestamp);
            parameters.Set(OwningEntityIdProperty, owningEntityId);
            AddParameters(parameters, deletion);
        }
    }
}