using ProtoBuf;

namespace SslChat.Common.Structures
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    [ProtoInclude(1000, typeof(ChatMessage))]
    [ProtoInclude(1001, typeof(ServerMessage))]
    public abstract class Message
    {
        protected Message(string content)
        {
            Content = content;
        }

        public string Content { get; set; }
    }
}