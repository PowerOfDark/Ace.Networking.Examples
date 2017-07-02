using System;
using ProtoBuf;

namespace SslChat.Common.Structures
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, ImplicitFirstTag = 10, SkipConstructor = true)]
    public class ServerMessage : Message
    {
        public ConsoleColor MessageColor;

        public ServerMessage(ConsoleColor color, string content) : base(content)
        {
            MessageColor = color;
        }
    }
}