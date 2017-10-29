using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RemoteHost.Logging;
using RemoteHost.Messages;

namespace RemoteHost.ProcessHosting
{
    public class PipedConsoleProcess : IProcess
    {
        public event EventHandler<ProcessEvent> EventOccured;

        private readonly string uniqueIdentifier = Guid.NewGuid().ToString("N");
        private readonly object processSync = new object();
        private readonly string assemblyLocation;
        private readonly CancellationTokenSource shutdownTokenSource = new CancellationTokenSource();

        private Process hostedProcess;
        private bool disposed;
        private NamedPipeServerStream writePipe;
        private NamedPipeServerStream readPipe;
        private BinaryWriter writer;

        public PipedConsoleProcess(string assemblyLocation)
        {
            this.assemblyLocation = assemblyLocation;
        }

        public bool IsRunning => hostedProcess != null;

        public void Start(StartupParameters arguments)
        {
            lock (processSync)
            {
                DisposeGuard();


                writePipe = new NamedPipeServerStream(uniqueIdentifier + "a", PipeDirection.Out);
                readPipe = new NamedPipeServerStream(uniqueIdentifier + "b", PipeDirection.In);

                var parameters = new StartupParameters {{StartupParameters.UniqueIdentifier, uniqueIdentifier}};
                foreach (var argument in arguments)
                {
                    parameters.Add(argument);
                }

                var xmlParameters = parameters.Serialize();

                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = assemblyLocation,
                    Arguments = xmlParameters,
                };

                hostedProcess = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };
                hostedProcess.Exited += HostedProcessOnExited;
                hostedProcess.Start();
                TraceHelper.WriteLine(TraceLevel.Verbose, $"started process {uniqueIdentifier}");

                writePipe.WaitForConnectionAsync(shutdownTokenSource.Token).Wait(1000);
                readPipe.WaitForConnectionAsync(shutdownTokenSource.Token).Wait(1000);
                if (!writePipe.IsConnected || !readPipe.IsConnected)
                {
                    var message = $"process {uniqueIdentifier} did not respond";
                    TraceHelper.WriteLine(TraceLevel.Error, message);
                    Dispose();
                    throw new Exception(message);
                }
                writer = new BinaryWriter(writePipe);

                TraceHelper.WriteLine(TraceLevel.Verbose, $"process {uniqueIdentifier} has started listening");
                //Task.Factory.StartNew(ReadFromPipe, shutdownTokenSource.Token);
            }
        }

        public void Send(byte[] bytes)
        {
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        //private void ReadFromPipe()
        //{
        //    using (var reader = new BinaryReader(server))
        //    while (!shutdownTokenSource.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            var buffer = reader.ReadInt32();
        //            var result = reader.ReadBytes(buffer);
        //            OnEventOccured(new ProcessEvent(ProcessEventType.DataReceived, result));
        //        }
        //        catch (Exception e)
        //        {
        //            TraceHelper.WriteLine(TraceLevel.Error, e.ToString());
        //        }

        //    }
        //}

        private void HostedProcessOnExited(object sender, EventArgs eventArgs)
        {
            var exitCode = hostedProcess?.ExitCode;
            TraceHelper.WriteLine(TraceLevel.Verbose, $"process {uniqueIdentifier} has exited with code {exitCode}");
            Dispose();
            OnEventOccured(new ProcessEvent(ProcessEventType.ProcessStopped, null));
        }

        public void Kill()
        {
            lock (processSync)
            {
                DisposeGuard();
                if (hostedProcess == null)
                {
                    TraceHelper.WriteLine(TraceLevel.Warning, $"attempted to kill process {uniqueIdentifier} when no process is running");
                    return;
                }

                hostedProcess.Kill();
                TraceHelper.WriteLine(TraceLevel.Warning, $"killed process {uniqueIdentifier} pid {hostedProcess.Id}");
                hostedProcess.Dispose();
                hostedProcess = null;
            }
        }

        private void DisposeGuard([CallerMemberName] string callerName = "")
        {
            if (disposed)
            {
                throw new ObjectDisposedException(callerName, $"cannot call {callerName} on a disposed instance");
            }
        }
        
        protected virtual void OnEventOccured(ProcessEvent e)
        {
            EventOccured?.Invoke(this, e);
        }

        public void Dispose()
        {
            shutdownTokenSource.Cancel();
            hostedProcess?.Dispose();
            hostedProcess = null;
            writePipe?.Dispose();
            writePipe = null;
            writer?.Dispose();
            writer = null;
            disposed = true;
        }
    }
}