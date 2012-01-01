using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using DeleporterCore.Configuration;
using DeleporterCore.SelfHosting;

namespace DeleporterCore
{
    public static class DeleporterUtilities
    {
        public static bool LocalPortIsAvailable(int port) {
            var localhost = Dns.GetHostAddresses("localhost")[0];

            try {
                var sock = new Socket(localhost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(localhost, port);
                if (sock.Connected) // RemotingPort is in use and connection is successful
                {
                    sock.Disconnect(false);
                    sock.Dispose();
                    return false;
                    
                }

                throw new Exception("Not connected to port ... but no Exception was thrown?");
            } catch (SocketException ex) {
                if (ex.ErrorCode == 10061) // RemotingPort is unused and could not establish connection 
                    return true;
                throw ex;
            }
        }


        /// <summary>
        ///   Requests the home page via WebClient.
        /// </summary>
        public static void PrimeServerHomepage() {
            Logger.Log("Priming Server Homepage...");
            using (var wc = new WebClient()) {
                try {
                    wc.DownloadString(DeleporterConfiguration.SiteBaseUrl);
                } catch (WebException webException) {
                    var responseStream = webException.Response.GetResponseStream();
                    var streamReader = new StreamReader(responseStream);

                    Logger.Log("Failed to prime the server. {0}", webException.Message);

                    var message = string.Format("Failed to prime the server. {0} {1}", webException.Message, streamReader.ReadToEnd());
                    throw new Exception(message);
                }
            }
            Logger.Log("Finished Priming Server Homepage");
        }

        public static void WaitForLocalPortToBecomeAvailable(int port, int sleepTimeInMilliseconds = 100, int timesToCheck = 100) {
            for (var i = 0; i < timesToCheck; i++) {
                var portIsAvailable = LocalPortIsAvailable(port);
                if (portIsAvailable) return;

                Thread.Sleep(sleepTimeInMilliseconds);
            }
            throw new Exception(
                    string.Format("Tried waiting for local port {0} to become available for {1} seconds, but it's still unavailable", port,
                                  (sleepTimeInMilliseconds * timesToCheck / 1000.0)));
        }

        public static void WaitForLocalPortToBecomeUnavailable(int port, int sleepTimeInMilliseconds = 100, int timesToCheck = 100) {
            for (var i = 0; i < timesToCheck; i++) {
                var portIsAvailable = LocalPortIsAvailable(port);
                if (portIsAvailable == false) return; // we're done waiting, the port is not available

                Thread.Sleep(sleepTimeInMilliseconds); // let's keep waiting
            }
            throw new Exception(
                    string.Format("Tried waiting for local port {0} to become unavailable for {1} seconds, but it's still available", port,
                                  (sleepTimeInMilliseconds * timesToCheck / 1000.0)));
        }


        public static void RecycleServerAppDomain()
        {
            var currentAssemblyCodeBase = Assembly.GetExecutingAssembly().CodeBase;
            var currentAssemblyDirectory = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(currentAssemblyCodeBase).Path));

            string testServerRootDirectory = Path.GetFullPath(Path.Combine(currentAssemblyDirectory, DeleporterConfiguration.RelativePathToWebApp));
            string webConfigFileName = Path.Combine(testServerRootDirectory, "Web.config");

            File.SetLastWriteTime(webConfigFileName, DateTime.Now);
        }

    }
}