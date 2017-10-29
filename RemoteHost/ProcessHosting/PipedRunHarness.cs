using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using RemoteHost.Messages;
using RemoteHost.Serialization;

namespace RemoteHost.ProcessHosting
{
    public abstract class PipedRunHarness : IRunHarness
    {
        private readonly CancellationTokenSource shutdownTokenSource = new CancellationTokenSource();
        private readonly object resultSync = new object();

        private IMessageSerializer serializer;
        private string namedPipe;
        private NamedPipeClientStream readPipe;
        private readonly Queue<RemoteHostMessage> toSend = new Queue<RemoteHostMessage>();
        private object target;

        public void Run(string[] args)
        {
            var parameters = GetParameters(args);
            serializer = GetSerializer(parameters);
            namedPipe = GetNamedPipe(parameters);
            target = GetInstance(parameters);

            using (readPipe = new NamedPipeClientStream(".", namedPipe + "a", PipeDirection.In))
            using (var writePipe = new NamedPipeClientStream(".", namedPipe + "b", PipeDirection.Out))
            {
                readPipe.Connect(1000);
                writePipe.Connect(1000);
                if (!readPipe.IsConnected || !writePipe.IsConnected)
                {
                    throw new Exception("Failed to connect to pipe " + namedPipe);
                }
                Debugger.Launch();
                Task.Factory.StartNew(ListenForMessages, shutdownTokenSource.Token);

                using (var writer = new BinaryWriter(writePipe))
                while (!shutdownTokenSource.IsCancellationRequested)
                {
                    lock (resultSync)
                    {
                        while (toSend.Any())
                        {
                            var result = toSend.Dequeue();
                            var message = serializer.Serialize(result);
                            try
                            {
                                writer.Write(message.Length);
                                writer.Write(message);
                            }
                            catch (IOException)
                            {
                                return;
                            }
                        }

                        Monitor.Wait(resultSync);
                    }
                }
            }
        }

        private void ListenForMessages()
        {
            var reader = new BinaryReader(readPipe);
            while (!shutdownTokenSource.IsCancellationRequested)
            {
                int bufferSize;
                byte[] buffer;
                try
                {
                    bufferSize = reader.ReadInt32();
                    buffer = reader.ReadBytes(bufferSize);
                }
                catch (EndOfStreamException)
                {
                    shutdownTokenSource.Cancel();
                    lock (resultSync)
                    {
                        Monitor.PulseAll(resultSync);
                    }
                    return;
                }

                var message = serializer.Deserialize(buffer);
                if (message is ShutdownMessage)
                {
                    SendMessage(message);
                    shutdownTokenSource.CancelAfter(1000);
                }
                else if (message is CallMethodMessage)
                {
                    var result = CallMethod(message);
                    SendMessage(result);
                }
            }
        }

        protected virtual RemoteHostMessageResult CallMethod(RemoteHostMessage message)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                return new RemoteHostMessageResult
                {
                    Exception = e,
                    MessageIdentifier = message.MessageIdentifier
                };
            }
        }

        private void SendMessage(RemoteHostMessage message)
        {
            lock (resultSync)
            {
                toSend.Enqueue(message);
                Monitor.PulseAll(resultSync);
            }
        }

        protected virtual object GetInstance(StartupParameters parameters)
        {
            var hostedtype = parameters[StartupParameters.HostedType];
            var type = Type.GetType(hostedtype);
            if (type == null)
            {
                throw new ArgumentException("Could not find instance of type " + hostedtype);
            }

            var instance = Activator.CreateInstance(type);
            if (instance == null)
            {
                throw new ArgumentException("Could not create instance of type " + hostedtype);
            }

            return instance;
        }

        protected virtual string GetNamedPipe(StartupParameters parameters)
        {
            var namedPipe = parameters[StartupParameters.UniqueIdentifier];
            if (string.IsNullOrWhiteSpace(namedPipe))
            {
                throw new ArgumentException("Invalid pipe name");
            }
            return namedPipe;
        }

        protected virtual IMessageSerializer GetSerializer(StartupParameters parameters)
        {
            var serializerName = parameters[StartupParameters.MessageSerializer];
            var type = Type.GetType(serializerName);
            if (type == null)
            {
                throw new ArgumentException("Could not find serializer of type " + serializerName);
            }

            var instance = Activator.CreateInstance(type) as IMessageSerializer;
            if (instance == null)
            {
                throw new ArgumentException("Could not create serializer of type " + serializerName);
            }

            return instance;
        }

        protected virtual StartupParameters GetParameters(string[] args)
        {
            var pipeArguments = args[0];
            var startup = Convert.FromBase64String(pipeArguments);
            var arguments = new XmlSerializer(typeof(StartupParameters));
            StartupParameters parameters;
            using (var memoryStream = new MemoryStream(startup, false))
            {
                memoryStream.Position = 0;
                parameters = (StartupParameters) arguments.Deserialize(memoryStream);
            }
            return parameters;
        }
    }
}