using System;
using System.Xml.Serialization;

namespace RemoteHost.Messages
{
    [Serializable]
    [XmlInclude(typeof(ShutdownMessage))]
    [XmlInclude(typeof(CallMethodMessage))]
    [XmlRoot("Message")]
    public abstract class RemoteHostMessage
    {
        [XmlElement]
        public Guid MessageIdentifier = Guid.NewGuid();
    }
}
