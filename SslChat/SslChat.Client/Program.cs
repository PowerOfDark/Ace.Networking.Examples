using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SslChat.Common;
using SslChat.Common.Packets;
using SslChat.Common.Structures;
using Ace.Networking.Structures;
using System.Threading;

namespace Ace.Networking.Example.Client
{
    internal class Program
    {
        public static Connection Connection;
        public static HistoryList<Message> Messages;
        public static int LastMsgHeight = 0;
        public const int ChatLineBreak = 3;

        public static object ConsoleSyncRoot = new object();

        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.InputEncoding = Encoding.Unicode;

            do
            {
                Console.Write("Enter server endpoint (localhost:12345): ");
                var str = Console.ReadLine();

                var arg = str.Split(new[] {":"}, StringSplitOptions.RemoveEmptyEntries);
                var host = arg[0];
                if (arg.Length != 2 || !int.TryParse(arg[1], out int port))
                {
                    continue;
                }
                var client = new TcpClient();
                Console.Write("Connecting... ");
                try
                {
                    client.ConnectAsync(host, port).Wait(5000);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                    client.Dispose();
                    continue;
                }
                Console.WriteLine("OK");

                Console.Write("Enter your username: ");
                var username = Console.ReadLine().ToLower();
                while (!File.Exists($"{username}.pfx"))
                {
                    Console.WriteLine(
                        $"Please place your client certificate in the current directory as {username}.pfx");
                    Console.WriteLine("Press ENTER to continue");
                    Console.ReadLine();
                }
                Console.Write("Enter certificate password (or leave blank): ");

                var pass = Console.ReadLine();
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine(new string('#', Console.WindowWidth));

                X509Certificate2 cert;
                try
                {
                    if (string.IsNullOrEmpty(pass))
                    {
                        cert = new X509Certificate2($"{username}.pfx");
                    }
                    else
                    {
                        cert = new X509Certificate2($"{username}.pfx", pass);
                        pass = null;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Certificate error: {e.Message}");
                    continue;
                }


                var cfg = MicroProtocolConfiguration.Instance;
                var ssl = cfg.GetClientSslFactory(host, cert);
                Connection = new Connection(client, cfg, ssl);
                var handshakeTask = Connection.Receive<S2C_Handshake>(TimeSpan.FromSeconds(5));
                try
                {
                    Connection.Initialize();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Intialization error: {e.Message}");
                    Connection.Close();
                    continue;
                }

                try
                {
                    var res = handshakeTask.Result;
                    Console.WriteLine(res.Message);
                    var response = new C2S_Handshake(MicroProtocolConfiguration.VersionString,
                        RuntimeInformation.OSDescription, username);
                    var handshakeResTask =
                        Connection.SendReceive<C2S_Handshake, S2C_HandshakeResult>(response, TimeSpan.FromSeconds(5));
                    var result = handshakeResTask.Result;
                    if (!result.Success)
                    {
                        Console.WriteLine(result.ErrorMessage);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
            } while (Connection == null || !Connection.Connected);

            Connection.Disconnected += Connection_Disconnected;

            Connection.On<S2C_InitialMessageList>(DisplayInitialMessages);
            Connection.On<S2C_NewMessage>(DisplayNewMessage);
            Connection.On<S2C_PingRequest>(HandlePing);

            Messages = new HistoryList<Message>(Console.LargestWindowHeight);
            try
            {
                Connection.SendReceive<C2S_JoinChat, S2C_InitialMessageList>(new C2S_JoinChat()).Wait();
                Thread.Sleep(500);
            }
            catch { }

            while (Connection.Connected)
            {
                lock (ConsoleSyncRoot)
                {
                    Console.SetCursorPosition(0, Console.WindowHeight - ChatLineBreak);
                    var empty = '>' + new string(' ', Console.WindowWidth * ChatLineBreak - 1);
                    Console.Write(empty);
                    Console.SetCursorPosition(1, Console.WindowHeight - ChatLineBreak);
                }
                var str = Console.ReadLine();
                if (!Connection.Connected)
                {
                    break;
                }
                Connection.Send(new C2S_SendMessage(str));
            }
        }

        private static object HandlePing(Connection connection, S2C_PingRequest payload)
        {
            return new C2S_PingAnswer(DateTime.Now);
        }

        public static void AddNewMessage(Message msg)
        {
            lock (Messages)
            {
                Messages.Add(msg);
            }
        }

        private static object DisplayNewMessage(Connection connection, S2C_NewMessage payload)
        {
            AddNewMessage(payload.Message);
            RenderMessages();
            return null;
        }

        private static object DisplayInitialMessages(Connection connection, S2C_InitialMessageList payload)
        {
            Console.Clear();
            foreach (var msg in payload.Messages)
            {
                AddNewMessage(msg);
            }
            RenderMessages();
            return null;
        }

        public static string Format(Message msg)
        {
            if (msg is ServerMessage sm)
            {
                return sm.Content;
            }
            if (msg is ChatMessage cm)
            {
                return $"{cm.From}> {cm.Content}";
            }
            return "";
        }

        private static void RenderMessages()
        {
            lock (Messages)
            {
                lock (ConsoleSyncRoot)
                {
                    var cl = Console.CursorLeft;
                    var ct = Console.CursorTop;
                    var linesLeft = Console.WindowHeight - (ChatLineBreak+1);
                    var last = Messages.Container.Last;
                    var width = Console.WindowWidth;

                    while (last != null)
                    {
                        var chars = Format(last.Value).Length;
                        var lines = chars / width + (chars % width > 0 ? 1 : 0);
                        if ((linesLeft - lines) < 0 || last.Previous == null)
                        {
                            break;
                        }
                        linesLeft -= lines;
                        last = last.Previous;
                    }

                    Console.SetCursorPosition(0, 0);

                    while (last != null)
                    {
                        var msg = last.Value;
                        var str = Format(last.Value);
                        if (str.Length % width > 0)
                        {
                            str += new string(' ', width - (str.Length % width));
                        }
                        if (msg is ServerMessage sm)
                        {
                            Console.ForegroundColor = sm.MessageColor;
                            Console.Write(str);
                            Console.ResetColor();
                        }
                        else if (msg is ChatMessage cm)
                        {
                            Console.Write(str);
                        }
                        last = last.Next;
                    }
                    for (int i = 0; i < linesLeft; i++)
                    {
                        Console.Write(new string(' ', width));
                    }

                    Console.SetCursorPosition(cl, ct);
                }
            }
        }

        private static void Connection_Disconnected(Connection connection, Exception exception)
        {
            lock (ConsoleSyncRoot)
            {
                Console.WriteLine("Disconnected! ({0})", exception?.Message);
                Console.WriteLine("Press ENTER to exit...");
            }
        }
    }
}