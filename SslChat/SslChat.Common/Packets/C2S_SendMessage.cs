using ProtoBuf;

namespace SslChat.Common.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class C2S_SendMessage
    {
        public C2S_SendMessage(string content)
        {
            Content = content;
        }

        protected C2S_SendMessage()
        {
        }

        public string Content { get; set; }
    }
}