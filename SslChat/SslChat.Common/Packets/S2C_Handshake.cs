using System.Runtime.InteropServices;
using ProtoBuf;

namespace SslChat.Common.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("CAF02D4B-8CE4-40B4-9793-2AFFED713863")]
    public class S2C_Handshake
    {
        public S2C_Handshake(string message)
        {
            Message = message;
        }

        protected S2C_Handshake()
        {
        }

        public string Message { get; set; }
    }
}