using System;
using ProtoBuf;

namespace SslChat.Common.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class C2S_PingAnswer
    {
        public C2S_PingAnswer(DateTime time)
        {
            Now = time;
        }

        protected C2S_PingAnswer()
        {
        }

        public DateTime Now { get; protected set; }
    }
}