using ProtoBuf;

namespace SslChat.Common.Structures
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public class ChatMessage : Message
    {
        public ChatMessage(string from, string content) : base(content)
        {
            From = from;
        }

        public string From { get; set; }
    }
}