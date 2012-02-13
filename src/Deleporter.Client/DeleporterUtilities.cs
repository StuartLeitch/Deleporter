using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using DeleporterCore.Configuration;

namespace DeleporterCore
{
    public static class DeleporterUtilities
    {
        public static int FindNextAvailableWebHostPort(int startingPort) {
            var portToTry = startingPort;
            var available = false;

            while (!available) {
                available = LocalPortIsAvailable(portToTry);

                if (available) continue;

                LoggerClient.Log("Port {0} was unavailable.  Trying {1}", portToTry, portToTry + 1);
                portToTry++;
            }

            return portToTry;
        }

        public static int FindNextAvailableRemotingPort(int startingPort) {
            var portToTry = startingPort;
            var available = false;

            while (!available) {
                available = LocalRemotingPortIsAvailable(portToTry);

                if (available) continue;

                LoggerClient.Log("Remoting Port {0} was unavailable.  Trying {1}", portToTry, portToTry + 1);
                portToTry++;
            }

            return portToTry;
        }

        public static bool LocalPortIsAvailable(int port) {
            var localhost = Dns.GetHostAddresses("localhost").First(x => x.AddressFamily == AddressFamily.InterNetwork);

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

        public static bool LocalRemotingPortIsAvailable(int port) {
            IPGlobalProperties globalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] activeListeners = globalProperties.GetActiveTcpListeners();
            return activeListeners.All(conn => conn.Port != port);
        }

        /// <summary>
        ///   Requests the home page via WebClient.
        /// </summary>
        public static void PrimeServerHomepage() {
            LoggerClient.Log("Priming Server Homepage...");
            using (var wc = new WebClient()) {
                try {
                    wc.DownloadString(DeleporterConfiguration.SiteBaseUrl);
                } catch (WebException webException) {
                    if (webException.Response == null) {
                        throw new InvalidOperationException("Failed to Prime Server. There was no response - if not SelfHosting, please make sure web application is running.");
                    }
                    var responseStream = webException.Response.GetResponseStream();
                    var streamReader = new StreamReader(responseStream);

                    LoggerClient.Log("Failed to prime the server. {0}", webException.Message);

                    var message = string.Format("Failed to prime the server. {0} {1}", webException.Message, streamReader.ReadToEnd());
                    throw new Exception(message);
                }
            }
            LoggerClient.Log("Finished Priming Server Homepage");
        }

        public static void RecycleServerAppDomain() {
            DeleporterConfiguration.RecycleWebServerAppDomain();
        }

        /// <summary>
        ///   Depending on the test runner, it is possible that tests may be aborted without proper cleanup. Ports may be left unavailable for a while. Work around this by getting fresh ports if needed.
        /// </summary>
        public static void IterateWebAndRemotingPortsIfNeeded() {
            IterateRemotingPortIfNeeded();
            IterateWebHostPortIfNeeded();
        }

        public static void IterateRemotingPortIfNeeded() {
            var remotingPort = FindNextAvailableRemotingPort(DeleporterConfiguration.RemotingPort);
            if (remotingPort != DeleporterConfiguration.RemotingPort)
                DeleporterConfiguration.UpdateRemotingPortInWebConfig(remotingPort);
        }

        public static void IterateWebHostPortIfNeeded() {
            var webHostPort = FindNextAvailableWebHostPort(DeleporterConfiguration.WebHostPort);
            if (webHostPort != DeleporterConfiguration.WebHostPort)
                DeleporterConfiguration.UpdateWebHostPortInWebConfig(webHostPort);
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
    }
}