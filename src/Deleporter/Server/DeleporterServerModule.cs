using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Web;
using DeleporterCore.Configuration;

namespace DeleporterCore.Server
{
    public class DeleporterServerModule : IHttpModule
    {
        private static int _isInitialRequest;
        private IChannel _remotingChannel;

        public void Init(HttpApplication app)
        {
            // Handle initialization on first request so we can conditionally activate based on the port number.
            Interlocked.Exchange(ref _isInitialRequest, 1);
            app.PostMapRequestHandler += this.Context_BeginRequest;
            LoggerServer.LoggingEnabled = true;
        }

        public void Context_BeginRequest(object sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _isInitialRequest, 0) != 1) return;

            if (!CurrentPortMatchesDeleporterSetting((HttpApplication)sender) || RemotingChannelExists()) return;

            if (WasCompiledInDebugMode(sender))
            {
                // Start listening for connections
                RemotingConfiguration.RegisterWellKnownServiceType(typeof(DeleporterService),
                                                                   DeleporterConfiguration.ServiceName,
                                                                   WellKnownObjectMode.Singleton);
                this._remotingChannel = DeleporterConfiguration.CreateChannel();
                LoggerServer.Log("Registering remoting channel on port {0}", DeleporterConfiguration.RemotingPort);
                ChannelServices.RegisterChannel(this._remotingChannel, false);
            }
            else
            {
                var thisAssembly = Assembly.GetExecutingAssembly().GetName();

                throw new InvalidOperationException(
                       string.Format("You should not enable Deleporter on production web servers. As a security precaution, Deleporter won't run if your ASP.NET application was compiled in Release mode. You need to remove DeleporterServerModule from your Web.config file. If you need to bypass this, the only way is to edit the Deleporter source code and remove this check. Assembly name {0} Version {1}", thisAssembly.Name, thisAssembly.Version));
            }
        }

        private static bool RemotingChannelExists()
        {
            var exists = false;

            if (ChannelServices.RegisteredChannels.Any())
            {
                var tcpChannel = ChannelServices.GetChannel("tcp") as TcpChannel;
                if (tcpChannel != null)
                {
                    var channelDataStore = tcpChannel.ChannelData as ChannelDataStore;
                    if (channelDataStore != null)
                    {
                        exists =
                                channelDataStore.ChannelUris.Any(
                                        x => x.EndsWith(string.Format(":{0}", DeleporterConfiguration.RemotingPort.ToString())));
                    }
                }
            }
            return exists;
        }

        private static bool CurrentPortMatchesDeleporterSetting(HttpApplication httpApplication)
        {
            // Only spin up the remoting channel if we are running on the same port as the settings
            var iisPort = int.Parse(httpApplication.Request.ServerVariables["SERVER_PORT"]);
            LoggerServer.Log("{0} - web.config WebHostPort: {1} running port: {2}", 
                DeleporterConfiguration.WebHostPort == iisPort ? "Match" : "MisMatch", DeleporterConfiguration.WebHostPort, iisPort);
            return DeleporterConfiguration.WebHostPort == iisPort;
        }

        #region Checking for debug mode
        private static bool WasCompiledInDebugMode(object value)
        {
            // In case the app class is auto-generated from a Global.asax file, check its base classes too, going down until we hit ASP.NET itself
            var assembliesToCheck =
                    GetInheritanceChain(value.GetType()).Select(x => x.Assembly).TakeWhile(
                            x => x != typeof(HttpApplication).Assembly).Distinct().ToList();

            var wasCompiledInDebugMode = assembliesToCheck.Any(AssemblyWasCompiledInDebugMode);

            if (!wasCompiledInDebugMode) {
                LoggerServer.Log("No assemblies found to be in Debug Mode - List of Assemblies:");
                assembliesToCheck.ForEach(x => LoggerServer.Log(x.Location));
            }

            return wasCompiledInDebugMode;
        }

        private static bool AssemblyWasCompiledInDebugMode(Assembly assembly)
        {
            var debuggableAttrib = assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().SingleOrDefault();
            var assemblyWasCompiledInDebugMode = debuggableAttrib != null && debuggableAttrib.IsJITTrackingEnabled;
            return assemblyWasCompiledInDebugMode;
        }

        private static IEnumerable<Type> GetInheritanceChain(Type type)
        {
            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }
        #endregion

        public void Dispose()
        {
            if (this._remotingChannel != null)
            {
                LoggerServer.Log("Disposing of Remoting Channel");
                ChannelServices.UnregisterChannel(this._remotingChannel);
            }

            LoggerServer.Dispose();
        }
    }
}