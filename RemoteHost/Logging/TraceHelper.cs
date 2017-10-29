using System.Diagnostics;

namespace RemoteHost.Logging
{
    public static class TraceHelper
    {
        public static readonly TraceSwitch TraceSwitch = new TraceSwitch("RemoteHostSwitch", "Remote host info");

        public static void WriteLine(TraceLevel traceLevel, string message)
        {
#if TRACE
            if (TraceSwitch.Level >= traceLevel)
            {
                Trace.WriteLine(message, "RemoteHost");
            }
#endif
        }
    }
}