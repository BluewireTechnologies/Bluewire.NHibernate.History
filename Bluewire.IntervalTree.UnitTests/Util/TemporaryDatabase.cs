using System;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Tool.hbm2ddl;

namespace Bluewire.IntervalTree.UnitTests.Util
{
    /// <summary>
    /// In-memory per-session test database instance. 
    /// </summary>
    /// <remarks>
    /// Faster than PersistentDatabase when only a single session will use it.
    /// </remarks>
    public class TemporaryDatabase
    {
        private readonly Configuration cfg;
        private ISessionFactory sessionFactory;

        private TemporaryDatabase()
        {
            cfg = new Configuration();
            cfg.DataBaseIntegration(d =>
            {
                d.Dialect<SQLiteDialect>();
                d.ConnectionString = "Data Source=:memory:;Version=3;New=true";
                d.ConnectionReleaseMode = ConnectionReleaseMode.OnClose;
            });
        }

        public static TemporaryDatabase Configure(Action<Configuration> configure)
        {
            var db = new TemporaryDatabase();
            db.InitConfiguration(configure);
            return db;
        }

        private void InitConfiguration(Action<Configuration> configure)
        {
            configure(cfg);
            SchemaMetadataUpdater.QuoteTableAndColumns(cfg);
            sessionFactory = cfg.BuildSessionFactory();
        }

        public ISession CreateSession()
        {
            var session = sessionFactory.OpenSession();
            new SchemaExport(cfg).Execute(false, true, false, session.Connection, null);
            return session;
        }
    }
}
