using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RemoteHost;
using RemoteHost.Logging;
using RemoteHost.ProcessHosting;
using RemoteHost.RunHarness.NetFramework;
using RemoteHost.Serialization;

namespace RemostHost.Tests
{
    [TestClass]
    public class UnitTest1
    {
        private RemoteHosted<DemoClass> target;

        [TestInitialize]
        public void Init()
        {
            TraceHelper.TraceSwitch.Level = TraceLevel.Verbose;
            target = new RemoteHosted<DemoClass>(new PipedConsoleProcessFactory<Program>(), new XmlMessageSerializer());
        }

        [TestMethod]
        public void TestMethod1()
        {
            target.Start();
            var test = "awesome";
            var result = target.Call(x => x.EchoMethod(test, Test(), DateTime.UtcNow, 1, DateTime.Now));
            Assert.AreEqual("awesome", result);
        }

        private string Test()
        {
            return "dd";
        }
    }

    public class FooClass
    {
        public string EchoMethod(string bar, string v, DateTime aads, int? whatisUp, DateTime? d)
        {
            return bar;
        }
    }

    public class DemoClass : FooClass
    {
        public string Value { get; set; }

        public void CoolMethod(string foo)
        {

        }
    }
}
