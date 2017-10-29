using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoteHost.ProcessHosting;

namespace RemoteHost.RunHarness.NetFramework
{
    public class Program : PipedRunHarness
    {
        static void Main(string[] args)
        {
            var item = new Program();
            item.Run(args);
        }
    }
}
