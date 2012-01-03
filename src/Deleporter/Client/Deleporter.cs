using System.Linq;
using System;
using System.Collections;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using DeleporterCore.Configuration;
using DeleporterCore.Server;

namespace DeleporterCore.Client
{
    public static class Deleporter
    {
        private static bool _hasRegisteredChannel;
        private static readonly object _currentInstanceCreationLock = new object();
        private volatile static DeleporterService _currentInstance;

        public static void Run(Action codeToExecute)
        {
            LoggerClient.Log("Deleporting Action");
            var result = Current.ExecuteAction(new SerializableDelegate<Action>(codeToExecute));
            CopyFields(result.Delegate.Target, codeToExecute.Target);
        }

        public static T Run<T>(Func<T> codeToExecute)
        {
            LoggerClient.Log("Deleporting Func");

            var genericType = typeof(SerializableDelegate<>).MakeGenericType(typeof(Func<T>));
            var serializableDelegate = Activator.CreateInstance(genericType, codeToExecute);
            var result = Current.ExecuteFunction((SerializableDelegate<Func<T>>)serializableDelegate);

            CopyFields(result.DelegateCalled.Delegate.Target, codeToExecute.Target);

            return result.DelegateCallResult;
        }

        private static DeleporterService Current {
            get {
                if (_currentInstance == null) {
                    lock (_currentInstanceCreationLock) {
                        if (_currentInstance == null)
                            _currentInstance = CreateInstance();
                    }
                }
                return _currentInstance;
            }
        }

        private static void CopyFields<T>(T from , T to) where T : class
        {
            if (from == null || to == null)
                return;
            foreach (FieldInfo fieldInfo in from.GetType().GetFields())
                fieldInfo.SetValue(to, fieldInfo.GetValue(from));
        }

        private static DeleporterService CreateInstance()
        {
            if (!_hasRegisteredChannel)
            {
                ChannelServices.RegisterChannel(new TcpChannel(
                                                    new Hashtable { { "port", 0 }, { "typeFilterLevel", TypeFilterLevel.Full }, { "name", Guid.NewGuid().ToString() } },
                                                    new BinaryClientFormatterSinkProvider(),
                                                    new BinaryServerFormatterSinkProvider { TypeFilterLevel = TypeFilterLevel.Full }
                                                    ), false);
                RemotingConfiguration.RegisterWellKnownClientType(typeof(DeleporterService), DeleporterConfiguration.HostAddress);
                _hasRegisteredChannel = true;
            }
            var instance = new DeleporterService();
            try {
                instance.RegisterAssemblyProvider(new AssemblyProvider());
            }
            catch (SocketException socketException)
            {
                LoggerClient.Log("Failed to create a channel on port {0}", DeleporterConfiguration.RemotingPort);

                throw new Exception(string.Format("Deleporter client was unable to connect to the remoting port {0} to the server.  Likely causes: 1) RemotingPort or WebHostPort settings are not the same in both projects; 2) WebServer may not have been able to listen on the remoting port because something else is using the port. Try using another port.", DeleporterConfiguration.RemotingPort), socketException);
            }

            LoggerClient.Log("Created remoting channel on port {0}", DeleporterConfiguration.RemotingPort);

            return instance;
        }
    }
}