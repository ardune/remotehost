using System;
using RemoteHost.Messages;

namespace RemoteHost.ProcessHosting
{
    public interface IProcess : IDisposable
    {
        event EventHandler<ProcessEvent> EventOccured;
        
        bool IsRunning { get; }
        void Start(StartupParameters arguments);
        void Kill();
        void Send(byte[] bytes);
    }
}