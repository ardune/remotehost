namespace RemoteHost.ProcessHosting
{
    public class ProcessEvent
    {
        public ProcessEvent(ProcessEventType type, byte[] data)
        {
            Type = type;
            Data = data;
        }
        
        public ProcessEventType Type { get; }
        public byte[] Data { get; }
    }
}