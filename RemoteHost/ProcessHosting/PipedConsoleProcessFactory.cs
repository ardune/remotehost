using RemoteHost.Serialization;

namespace RemoteHost.ProcessHosting
{
    public class PipedConsoleProcessFactory<TRunHarness> : IProcessFactory
        where TRunHarness : IRunHarness
    {
        private readonly string assemblyLocation;

        public PipedConsoleProcessFactory()
        {
            assemblyLocation = typeof(TRunHarness).Assembly.Location;
        }

        public IProcess CreateProcess()
        {
            return new PipedConsoleProcess(assemblyLocation);
        }
    }
}