using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using RemoteHost.Logging;
using RemoteHost.Messages;
using RemoteHost.ProcessHosting;
using RemoteHost.Serialization;

namespace RemoteHost
{
    public class RemoteHosted<THosted> : IRemoteHosted<THosted>
        where THosted : class, new() 
    {
        private bool disposed;

        private readonly IProcessFactory processFactory;
        private readonly object remoteSync = new object();
        private readonly IMessageSerializer messageSerializer;
        private readonly Dictionary<Guid, RemoteHostMessageResult> results = new Dictionary<Guid, RemoteHostMessageResult>();

        private IProcess hostedProcess;

        public RemoteHosted(IProcessFactory processFactory, IMessageSerializer messageSerializer)
        {
            this.processFactory = processFactory;
            this.messageSerializer = messageSerializer;
        }

        public TResult Call<TResult>(Expression<Func<THosted, TResult>> methodCall)
        {
            lock (remoteSync)
            {
                DisposeGuard();
                var body = methodCall.Body as MethodCallExpression;
                if (body == null)
                {
                    throw new ArgumentException("expected a method call from " + methodCall);
                }

                if (body.Method.DeclaringType is THosted)
                {
                    throw new ArgumentException("expected a method call from the type " + typeof(THosted) +
                                                " instead got a method from " + body.Method.DeclaringType);
                }

                var parameters = ExtractParameters(methodCall.Parameters, body).ToArray();

                var callMethod = new CallMethodMessage
                {
                    Returns = true,
                    Arguments = parameters,
                    MethodName = body.Method.Name
                };

                var result = SynchroniousCall(callMethod);

                throw new NotImplementedException();
            }

        }

        private RemoteHostMessage SynchroniousCall(RemoteHostMessage message)
        {
            lock (remoteSync)
            {
                DisposeGuard();

                var bytes = messageSerializer.Serialize(message);

                hostedProcess.Send(bytes);

                while (!disposed && hostedProcess.IsRunning)
                {
                    if (results.ContainsKey(message.MessageIdentifier))
                    {
                        var result = results[message.MessageIdentifier];
                        results.Remove(message.MessageIdentifier);
                        if (result.Exception != null)
                        {
                            throw result.Exception;
                        }
                        return result;
                    }

                    Monitor.Wait(remoteSync);
                }
                return null;
            }
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }


        public void Start()
        {
            lock (remoteSync)
            {
                DisposeGuard();
                if (hostedProcess != null)
                {
                    TraceHelper.WriteLine(TraceLevel.Warning, "attempted to start process when one was already started");
                    return;
                }
                hostedProcess = processFactory.CreateProcess();
                hostedProcess.EventOccured += HostedProcessOnEventOccured;

                hostedProcess.Start(new StartupParameters
                {
                    {StartupParameters.MessageSerializer, messageSerializer.GetType().AssemblyQualifiedName},
                    {StartupParameters.HostedType, typeof(THosted).AssemblyQualifiedName},
                });
            }
        }

        private void HostedProcessOnEventOccured(object sender, ProcessEvent processEvent)
        {
            lock (remoteSync)
            {
                switch (processEvent.Type)
                {
                    case ProcessEventType.DataReceived:
                        var message = messageSerializer.Deserialize(processEvent.Data);
                        results[message.MessageIdentifier] = message as RemoteHostMessageResult;
                        Monitor.PulseAll(remoteSync);
                        return;
                    case ProcessEventType.ProcessStopped:
                        Monitor.PulseAll(remoteSync);
                        return;
                }
            }
        }

        // see: https://stackoverflow.com/a/3766713
        private static IEnumerable<object> ExtractParameters(IReadOnlyCollection<ParameterExpression> parameters, MethodCallExpression body)
        {
            var arguments = body.Arguments;
            foreach (var argument in arguments)
            {
                var lambda = Expression.Lambda(argument, parameters);
                var d = lambda.Compile();
                yield return d.DynamicInvoke(new object[1]);
            }
        }

        private void DisposeGuard([CallerMemberName] string callerName = "")
        {
            if (disposed)
            {
                throw new ObjectDisposedException(callerName, $"cannot call {callerName} on a disposed instance");
            }
        }
        
        public void Dispose()
        {
            lock (remoteSync)
            {
                hostedProcess?.Dispose();
                hostedProcess = null;
                disposed = true;
            }
        }
    }
}
