﻿using System;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Ace.Networking;
using Ace.Networking.MicroProtocol;
using Ace.Networking.MicroProtocol.SSL;
using Ace.Networking.ProtoBuf;

namespace SslChat.Common
{
    public class MicroProtocolConfiguration : ProtocolConfiguration
    {
        /// <summary>
        ///     Helper-field
        /// </summary>
        public const string VersionString = "v1.0.0";

        private static readonly object SingletonLock = new object();
        private static ProtocolConfiguration _instance;

        public static ProtocolConfiguration Instance
        {
            get
            {
                lock (SingletonLock)
                {
                    return _instance ?? (_instance = new MicroProtocolConfiguration());
                }
            }
        }


        public MicroProtocolConfiguration()
        {
            var serializer = new GuidProtoBufSerializer();
            PayloadEncoder = new MicroEncoder(serializer.Clone());
            PayloadDecoder = new MicroDecoder(serializer.Clone());
            RequireClientCertificate = true;
            CustomOutcomingMessageQueue = GlobalOutcomingMessageQueue.Instance;
            CustomIncomingMessageQueue = GlobalIncomingMessageQueue.Instance;
            Initialize();
        }

        protected override void Initialize()
        {
            if (IsInitialized) return;
            base.Initialize();
            GuidProtoBufSerializer.RegisterAssembly(typeof(MicroProtocolConfiguration).GetTypeInfo().Assembly);
        }

        public override ClientSslStreamFactory GetClientSslFactory(string targetCommonCame = "",
            X509Certificate certificate = null)
        {
            if (UseSsl & RequireClientCertificate && certificate == null)
                throw new ArgumentNullException(nameof(certificate));
            return new ClientSslStreamFactory(targetCommonCame, certificate);
        }

        public override ServerSslStreamFactory GetServerSslFactory(X509Certificate certificate = null)
        {
            if (UseSsl && certificate == null)
                throw new ArgumentNullException(nameof(certificate));
            return new ServerSslStreamFactory(certificate, RequireClientCertificate);
        }
    }
}