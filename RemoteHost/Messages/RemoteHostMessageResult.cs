using System;

namespace RemoteHost.Messages
{
    [Serializable]
    public class RemoteHostMessageResult : RemoteHostMessage
    {
        public Exception Exception;
    }
}