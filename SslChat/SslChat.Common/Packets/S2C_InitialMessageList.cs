using System.Collections.Generic;
using SslChat.Common.Structures;
using ProtoBuf;

namespace SslChat.Common.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class S2C_InitialMessageList
    {
        public S2C_InitialMessageList(ICollection<Message> messages)
        {
            Messages = messages;
        }

        protected S2C_InitialMessageList()
        {
        }

        public ICollection<Message> Messages { get; protected set; }
    }
}