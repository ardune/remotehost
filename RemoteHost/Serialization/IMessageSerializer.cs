using RemoteHost.Messages;

namespace RemoteHost.Serialization
{
    public interface IMessageSerializer
    {
        byte[] Serialize(RemoteHostMessage data);
        RemoteHostMessage Deserialize(byte[] data);
    }
}