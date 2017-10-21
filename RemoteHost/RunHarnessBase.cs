using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteHost
{
    public abstract class RunHarnessBase : IRunHarness
    {
        private readonly CancellationTokenSource shutdownTokenSource = new CancellationTokenSource();
        private readonly object resultSync = new object();
        private readonly Queue<MessageResult> results = new Queue<MessageResult>();

        public void Start(string[] args)
        {
            Task.Factory.StartNew(ListenForMessages, shutdownTokenSource.Token);
            while (!shutdownTokenSource.IsCancellationRequested)
            {
                lock (resultSync)
                {
                    ReturnResults();
                    Monitor.Wait(resultSync);
                    ReturnResults();
                }
            }
        }

        private void ReturnResults()
        {
            while (results.Any())
            {
                var result = results.Dequeue();
                Console.WriteLine(result.CallIdentifier);
                Console.WriteLine(result.Result);
                Console.WriteLine(result.Exception);
            }
        }
        
        private void ListenForMessages()
        {
            while (!shutdownTokenSource.IsCancellationRequested)
            {
                var identifier = Console.ReadLine();
                var messageType = Console.ReadLine();
                var argument = Console.ReadLine();

                Task.Factory.StartNew(() =>
                {
                    Enum.TryParse(messageType, out MessageType type);

                    try
                    {
                        var result = ProcessMessage(new Message
                        {
                            CallIdentifier = identifier,
                            MessageType = type,
                            Argument = Convert.FromBase64String(argument)
                        });
                        Submit(result);
                    }
                    catch (Exception e)
                    {
                        Submit(new MessageResult
                        {
                            CallIdentifier = identifier,
                            Exception = e
                        });
                    }
                }, shutdownTokenSource.Token);
            }
        }
        
        private MessageResult ProcessMessage(Message message)
        {
            switch (message.MessageType)
            {
                case MessageType.LoadClass:
                    return LoadClass(message);
                case MessageType.Shutdown:
                    return Shutdown(message);
                default:
                    return new MessageResult
                    {
                        CallIdentifier = message.CallIdentifier
                    };
            }
        }

        protected virtual MessageResult Shutdown(Message message)
        {
            Debugger.Launch();
            shutdownTokenSource.Cancel();
            return new MessageResult
            {
                CallIdentifier = message.CallIdentifier,
                Result = "shutting down"
            };
        }

        protected virtual MessageResult LoadClass(Message message)
        {
            var assemblyDetails = Encoding.UTF8.GetString(message.Argument).Split('\n');
            var location = assemblyDetails[0];
            var fullyQualifiedAssemblyName = assemblyDetails[1];
            //var assembly = AppDomain.CurrentDomain.Load(location);
            //var type = assembly.GetType(fullyQualifiedAssemblyName);
            var type = Type.GetType(fullyQualifiedAssemblyName);



            return new MessageResult
            {
                CallIdentifier = message.CallIdentifier,
                Result = "I loaded " + type
            };
        }

        private void Submit(MessageResult result)
        {
            lock (resultSync)
            {
                results.Enqueue(result);

                Monitor.PulseAll(resultSync);
            }
        }
    }
}