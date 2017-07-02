using System.Runtime.InteropServices;
using ProtoBuf;

namespace SslChat.Common.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    [Guid("B32EFA49-9D59-4115-B49D-2D9B1071DF9A")]
    public class C2S_Handshake
    {
        public C2S_Handshake(string version, string os, string username)
        {
            VersionString = version;
            OS = os;
            Username = username;
        }

        protected C2S_Handshake()
        {
        }

        public string VersionString { get; protected set; }
        public string OS { get; protected set; }
        public string Username { get; protected set; }
    }
}