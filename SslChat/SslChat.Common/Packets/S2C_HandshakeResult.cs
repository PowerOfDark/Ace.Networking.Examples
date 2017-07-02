using System.Runtime.InteropServices;
using ProtoBuf;

namespace SslChat.Common.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("1A9330EC-F15C-4EF1-AFAC-2A4FEA9547D2")]
    public class S2C_HandshakeResult
    {
        public S2C_HandshakeResult(string error = "")
        {
            Success = string.IsNullOrWhiteSpace(error);
            ErrorMessage = error;
        }

        protected S2C_HandshakeResult()
        {
        }

        public bool Success { get; protected set; }
        public string ErrorMessage { get; protected set; }
    }
}