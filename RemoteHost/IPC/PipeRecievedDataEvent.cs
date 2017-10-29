namespace RemoteHost.IPC
{
    public class PipeRecievedDataEvent
    {
        public PipeRecievedDataEvent( byte[] data)
        {
            Data = data;
        }
        
        public byte[] Data { get; }
    }
}