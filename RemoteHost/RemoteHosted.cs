using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace RemoteHost
{
    public class RemoteHosted<THosted, TRunHarness> : IRemostHosted<THosted>
        where THosted : class, new() 
        where TRunHarness : class, IRunHarness
    {
        private bool isDisposed;
        private Process hostedProcess;
        private readonly string assemblyLocation;
        private readonly List<string> pending = new List<string>();
        private readonly Queue<string> buffer = new Queue<string>();
        private readonly Dictionary<string,MessageResult> results = new Dictionary<string,MessageResult>();

        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        private readonly object inputSync = new object();
        private readonly object bufferSync = new object();
        private readonly object resultSync = new object();


        public RemoteHosted()
        {
            assemblyLocation =  typeof(TRunHarness).Assembly.Location;
        }

        public TResult Call<TResult>(Expression<Func<THosted, TResult>> methodCall)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            if (hostedProcess == null)
            {
                StartProcess();
                InitilizeProcessEnvironment();
            }
        }

        public void Stop()
        {
            if (hostedProcess != null)
            {
                Execute(new Message
                {
                    MessageType = MessageType.Shutdown
                });
            }
        }

        private void InitilizeProcessEnvironment()
        {
            var type = string.Join("\n", typeof(THosted).Assembly.FullName, typeof(THosted).AssemblyQualifiedName);
            Execute(new Message
            {
                MessageType = MessageType.LoadClass,
                Argument = Encoding.UTF8.GetBytes(type)
            });
        }

        private MessageResult Execute(Message value)
        {
            lock (inputSync)
            {
                pending.Add(value.CallIdentifier);
                hostedProcess.StandardInput.WriteLine(value.CallIdentifier);
                hostedProcess.StandardInput.WriteLine(value.MessageType);
                hostedProcess.StandardInput.WriteLine(Convert.ToBase64String(value.Argument));
            }
            return WaitForResult(value);
        }

        private MessageResult WaitForResult(Message value)
        {
            var key = value.CallIdentifier;
            lock (resultSync)
            {
                while (!isDisposed)
                {
                    if (results.ContainsKey(key))
                    {
                        var result = results[key];
                        results.Remove(key);
                        if (result.Exception != null)
                        {
                            throw result.Exception;
                        }
                        return result;
                    }

                    Monitor.Wait(resultSync);
                }
                return null;
            }
        }


        private void StartProcess()
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                FileName = assemblyLocation
            };

            hostedProcess = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            hostedProcess.OutputDataReceived += HandleOutput;
            hostedProcess.Start();
            hostedProcess.BeginOutputReadLine();
        }

        private void HandleOutput(object sender, DataReceivedEventArgs e)
        {
            Trace.WriteLine("HandleOutput");
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                Trace.WriteLine("no data");
                return;
            }
            Trace.WriteLine(e.Data);
            lock (bufferSync)
            {
                buffer.Enqueue(e.Data);
            }
            TryParse();
        }

        private void TryParse()
        {
            var newResults = new List<MessageResult>();
            lock (bufferSync)
            {
                while (buffer.Count>=2)
                {
                    var identifier = buffer.Dequeue();
                    var result = buffer.Dequeue();
                    newResults.Add(new MessageResult
                    {
                        CallIdentifier = identifier,
                        Result = result
                    });
                }
            }

            lock (resultSync)
            {
                foreach (var messageResult in newResults)
                {
                    results[messageResult.CallIdentifier] = messageResult;
                }

                Monitor.PulseAll(resultSync);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    if (hostedProcess != null)
                    {
                        hostedProcess.Kill();
                        hostedProcess.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                hostedProcess = null;

                isDisposed = true;
                if (disposing)
                {
                    Monitor.PulseAll(resultSync);
                }
            }
        }
        
        ~RemoteHosted()
        {
            Dispose(false);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public sealed class MessageResult
    {
        public string CallIdentifier;
        public Exception Exception;
        public object Result;
    }

    public sealed class Message
    {
        public string CallIdentifier = Guid.NewGuid().ToString("N");
        public MessageType MessageType;
        public byte[] Argument = new byte[0];
    }

    public enum MessageType
    {
        NoOp,
        LoadClass,
        Shutdown
    }
}
