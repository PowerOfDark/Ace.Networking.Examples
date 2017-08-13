using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Ace.Networking;
using SslChat.Common;
using SslChat.Common.Packets;
using SslChat.Common.Structures;
using Ace.Networking.MicroProtocol.SSL;
using Ace.Networking.Structures;

namespace SslChat.Server
{
    internal class Program
    {
        public static TcpServer Server;

        public static HistoryList<Message> Messages = new HistoryList<Message>(50);
        public static int ChatClients;

        private static void Main(string[] args)
        {
            var cfg = MicroProtocolConfiguration.Instance;
            var certificate = new X509Certificate2("localhost.pfx", "foobar");
            var sslFactory = cfg.GetServerSslFactory(certificate);
            Server = new TcpServer(new IPEndPoint(IPAddress.Loopback, 12345), cfg, sslFactory)
            {
                AcceptClient = Server_HandleNewClient
            };
            Server.ClientDisconnected += Server_ClientDisconnected;
            Server.ReceiveTimeout = TimeSpan.FromMilliseconds(5000);
            Server.IdleTimeout += Server_Timeout;
            Server.On<C2S_JoinChat>(JoinChat);
            Server.On<C2S_SendMessage>(SendMessage);
            Server.Start();

            AddNewMessage(new ServerMessage(ConsoleColor.Cyan,
                $"Welcome to the chat server [{MicroProtocolConfiguration.VersionString}]"));
            Console.WriteLine("Server started. Press ENTER to quit");
            Console.ReadLine();

            Console.Write("Stopping... ");
            AddNewMessage(new ServerMessage(ConsoleColor.Red, "Server shutting down..."));
            Server.Off(); //clear type handlers
            Server.ClientDisconnected -= Server_ClientDisconnected; //remove disconnect handler
            Thread.Sleep(1000);
            Server.Stop();
            Console.WriteLine("OK");
            Console.ReadLine();
        }

        private static object SendMessage(Connection connection, C2S_SendMessage payload)
        {
            
            if (connection.Data.Get("Connected", false))
            {
                var username = connection.Data.Get("Username", connection.Guid.ToString());
                AddNewMessage(new ChatMessage(username, payload.Content));
            }
            return null;
        }

        private static object JoinChat(Connection connection, C2S_JoinChat payload)
        {
            if (connection.Data.Get("Authorized", false))
            {
                Interlocked.Increment(ref ChatClients);
                var username = connection.Data.Get("Username", connection.Guid.ToString());
                AddNewMessage(new ServerMessage(ConsoleColor.Green, $"{username} joined the channel."));
                connection.Data["Connected"] = true; // after broadcasting the message
                List<Message> list;
                lock (Messages)
                {
                    list = Messages.Container.ToList();
                }
                list.Add(new ServerMessage(ConsoleColor.DarkGray,
                    $"There are currently {ChatClients - 1} other clients available."));
                return new S2C_InitialMessageList(list);
            }
            return null;
        }

        private static void AddNewMessage(Message msg)
        {
            var packet = new S2C_NewMessage(msg);
            lock (Messages)
            {
                Messages.Add(msg);
                foreach (var con in Server.Connections)
                {
                    if (con.Value.Connected && con.Value.Data.Get("Connected", false))
                    {
                        try
                        {
                            con.Value.Send(packet);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private static bool Server_HandleNewClient(Connection client)
        {
            var msg = $"Welcome to the chat server [{MicroProtocolConfiguration.VersionString}]";
            var handshakeTask =
                client.SendReceive<S2C_Handshake, C2S_Handshake>(new S2C_Handshake(msg), TimeSpan.FromSeconds(5));
            try
            {
                var result = handshakeTask.Result;
                var error = "";
                var subject = client.SslCertificates.RemoteCertificate.Subject;
                if (result.VersionString != MicroProtocolConfiguration.VersionString)
                {
                    error = "Invalid version";
                }
                else if (client.SslCertificates.RemotePolicyErrors != SslPolicyErrors.None ||
                         !BasicCertificateInfo.VerifyAttribute(subject,
                             CertificateAttribute.CommonName, result.Username))
                {
                    error = "Invalid certificate";
                }
                var ret = new S2C_HandshakeResult(error);
                client.Data["Authorized"] = ret.Success;
                if (ret.Success)
                {
                    var username =
                        BasicCertificateInfo.GetAttribute(subject,
                            CertificateAttribute.CommonName, result.Username);
                    client.Data["Username"] = username;
                }
                client.Send(ret);
                return ret.Success;
            }
            catch
            {
            }

            return false;
        }

        private static void Server_Timeout(Connection connection)
        {
            var req = new S2C_PingRequest(DateTime.Now);
            connection.SendRequest<S2C_PingRequest, C2S_PingAnswer>(req, TimeSpan.FromSeconds(10)).ContinueWith(t =>
            {
                var now = DateTime.Now;
                if (t.IsFaulted || t.IsCanceled)
                {
                    connection.Close();
                    return;
                }
                var ping = (now - req.Now).TotalMilliseconds;
                //Console.WriteLine($"ping: {ping:0.00}ms");
                //TODO: Implement proper pinging
            });
        }

        private static void Server_ClientDisconnected(Connection connection, Exception exception)
        {
            if (connection.Data.Get("Connected", false))
            {
                Interlocked.Decrement(ref ChatClients);
                var username = connection.Data.Get("Username", connection.Guid.ToString());
                AddNewMessage(new ServerMessage(ConsoleColor.Red, $"{username} left the channel."));
            }
            Console.WriteLine($"Connection [{connection.Identifier}] closed");
        }
    }
}