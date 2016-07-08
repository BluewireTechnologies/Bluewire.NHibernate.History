using System;
using Bluewire.NHibernate.Audit.UnitTests;
using log4net.Config;
using log4net.Core;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

[assembly: EnableLog4Net]

namespace Bluewire.NHibernate.Audit.UnitTests
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class EnableLog4NetAttribute :  Attribute, ITestAction
    {

        public void AfterTest(ITest testDetails)
        {
        }

        public void BeforeTest(ITest testDetails)
        {
            BasicConfigurator.Configure(
                new log4net.Appender.ConsoleAppender {
                    Threshold = Level.Warn,
                    Layout = new log4net.Layout.SimpleLayout()
                });
        }

        public ActionTargets Targets
        {
            get { return ActionTargets.Suite; }
        }
    }
}
