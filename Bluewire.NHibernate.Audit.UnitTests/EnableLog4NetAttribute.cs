using System;
using Bluewire.NHibernate.Audit.UnitTests;
using log4net.Config;
using NUnit.Framework;

[assembly: EnableLog4Net]

namespace Bluewire.NHibernate.Audit.UnitTests
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class EnableLog4NetAttribute :  Attribute, ITestAction
    {

        public void AfterTest(TestDetails testDetails)
        {
        }

        public void BeforeTest(TestDetails testDetails)
        {
            BasicConfigurator.Configure();
        }

        public ActionTargets Targets
        {
            get { return ActionTargets.Suite; }
        }
    }
}
