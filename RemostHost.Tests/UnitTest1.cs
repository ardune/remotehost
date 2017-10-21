using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RemoteHost;
using RemoteHost.RunHarness.NetFramework;

namespace RemostHost.Tests
{
    [TestClass]
    public class UnitTest1
    {
        private RemoteHosted<DemoClass, Program> target;

        [TestInitialize]
        public void Init()
        {
            target = new RemoteHosted<DemoClass, Program>();
        }

        [TestMethod]
        public void TestMethod1()
        {
            target.Start();
            target.Stop();
        }
    }


    public class DemoClass
    {
        public string Value { get; set; }

        public void CoolMethod(string foo)
        {

        }

        public string EchoMethod(string bar)
        {
            return bar;
        }
    }
}
