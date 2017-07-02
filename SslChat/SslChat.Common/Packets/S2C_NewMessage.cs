using SslChat.Common.Structures;
using ProtoBuf;

namespace SslChat.Common.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class S2C_NewMessage
    {
        public S2C_NewMessage(Message message)
        {
            Message = message;
        }

        protected S2C_NewMessage()
        {
        }

        public Message Message { get; protected set; }
    }
}