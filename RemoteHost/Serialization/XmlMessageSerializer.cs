using System.IO;
using System.Xml.Serialization;
using RemoteHost.Messages;

namespace RemoteHost.Serialization
{
    public class XmlMessageSerializer : IMessageSerializer
    {
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(RemoteHostMessage));

        public byte[] Serialize(RemoteHostMessage data)
        {
            using (var memoryStream = new MemoryStream())
            {
                serializer.Serialize(memoryStream, data);
                return memoryStream.ToArray();
            }
        }

        public RemoteHostMessage Deserialize(byte[] data)
        {
            using (var memoryStream = new MemoryStream(data,false))
            {
                memoryStream.Position = 0;
                return (RemoteHostMessage)serializer.Deserialize(memoryStream);
            }
        }
    }
}