using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DeleporterCore.SelfHosting.SeleniumServer.Configuration;
using DeleporterCore.SelfHosting.Servers;

namespace DeleporterCore.SelfHosting.SeleniumServer.Servers
{
    public class SeleniumServer : IServer
    {

        private Process _seleniumServer;
        private bool _started;
        private static IServer _instance;
        public static IServer Instance { get { return _instance ?? (_instance = new SeleniumServer()); } }

        public bool Start()
        {
            if (this._started) return false;
            this._started = true;

            if (!DeleporterUtilities.LocalPortIsAvailable(DeleporterSeleniumServerConfiguration.SeleniumServerPort)) {
                Logger.Log("Selenium port {0} is being used. Attempt to start Selenium has been aborted. " 
                                        + "A previous instance may be running in which case we will just use that.",
                                        DeleporterSeleniumServerConfiguration.SeleniumServerPort);
                return false;
            }

            var javaExecutable = FileUtilities.TryToFindProgramFile("java.exe", "java");
            var seleniumServerJar = DeleporterSeleniumServerConfiguration.SeleniumServerJar;
            this.ThrowIfFilesDontExist(seleniumServerJar, javaExecutable);

            this._seleniumServer = new Process
            {
                    StartInfo =
                            {
                                    FileName = javaExecutable,
                                    Arguments =
                                            string.Format("-jar {0} -port {1}",
                                                          seleniumServerJar,
                                                          DeleporterSeleniumServerConfiguration.SeleniumServerPort),
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                            }
            };

            Logger.Log("Selenium Instance starting ... ");
            try {
                this._seleniumServer.Start();
            } catch (Exception ex) {
                Logger.Log("Couldn't start Selenium ... {0}", ex.Message);
                return false;
            }

            // 20 seconds max ... checking every 0.1 seconds
            DeleporterUtilities.WaitForLocalPortToBecomeUnavailable(DeleporterSeleniumServerConfiguration.SeleniumServerPort,
                                                                    100,
                                                                    200);
            Logger.Log("Selenium Started");
            return true;
        }

        public void Stop()
        {
            if (this._seleniumServer != null) {
                Logger.Log("Selenium Instance stopping ... ");
                this._seleniumServer.Kill();
                DeleporterUtilities.WaitForLocalPortToBecomeAvailable(DeleporterSeleniumServerConfiguration.SeleniumServerPort);
                Logger.Log("Selenium Stopped");
            }
        }

        private void ThrowIfFilesDontExist(string seleniumServerJar, string javaExecutable)
        {
            if (javaExecutable == null) {
                throw new FileNotFoundException(
                        "Java Could not be found on your system. You may optionally specify the directory for java.exe in your "
                        + "web.config (see documentation).");
            }

            if (seleniumServerJar == null) {
                throw new FileNotFoundException(
                        "SeleniumServerJar Could not be found on your system. You may optionally specify the directory for SeleniumServerJar in your "
                        + "web.config (see documentation).");
            }
        }
    }
}