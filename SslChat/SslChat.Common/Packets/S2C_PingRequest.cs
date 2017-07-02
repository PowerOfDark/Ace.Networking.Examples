using System;
using ProtoBuf;

namespace SslChat.Common.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class S2C_PingRequest
    {
        public S2C_PingRequest(DateTime time)
        {
            Now = time;
        }

        protected S2C_PingRequest()
        {
        }

        public DateTime Now { get; protected set; }
    }
}