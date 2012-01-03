using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DeleporterCore
{
    internal class LoggerServer : LoggerBase
    {
        private static LoggerServer _instance;

        private LoggerServer() : base("DeleporterServer.log") {}

        public static bool LoggingEnabled { set { Instance._loggingEnabled = value; } }

        private static LoggerServer Instance { get { return _instance ?? (_instance = new LoggerServer()); } }

        public static void Dispose() {
            Instance.DisposeLogger();
        }

        [DebuggerStepThrough]
        public static void Log(string message, params object[] args) {
            Instance.LogMessage(message, args);
        }
    }

    public class LoggerClient : LoggerBase
    {
        private static LoggerClient _instance;

        public LoggerClient() : base("DeleporterClient.log") {}

        public static bool LoggingEnabled { set { Instance._loggingEnabled = value; } }

        private static LoggerClient Instance { get { return _instance ?? (_instance = new LoggerClient()); } }

        public static void Dispose() {
            Instance.DisposeLogger();
        }

        [DebuggerStepThrough]
        public static void Log(string message, params object[] args) {
            Instance.LogMessage(message, args);
        }
    }

    public abstract class LoggerBase
    {
        protected bool _loggingEnabled;
        private readonly string _logFileName;
        private StreamWriter _logWriter;

        protected LoggerBase(string logFileName) {
            this._logFileName = logFileName;
        }

        private StreamWriter LogWriter {
            get {
                try {
                    if (this._logWriter != null)
                        return this._logWriter;
                    this._logWriter = new StreamWriter(Path.Combine(Path.GetTempPath(), this._logFileName), false);
                    this._logWriter.WriteLine("Starting log session {0} in {1}", DateTime.Now, this._logFileName);
                    this._logWriter.Flush();
                    return this._logWriter;
                } catch (Exception exception) {
                    Debug.WriteLine(exception);
                }

                return null;
            }
        }

        protected void DisposeLogger() {
            this.LogMessage("Disposing of {0}", this._logFileName);
            this._loggingEnabled = false;
            this._logWriter.Dispose();
        }

        protected void LogMessage(string message, params object[] args) {
            if (!this._loggingEnabled || this.LogWriter == null)
                return;

            Debug.WriteLine(message, args);

            try {
                this.LogWriter.WriteLine(String.Format("{0:D2}:{1:D3}", DateTime.Now.Second, DateTime.Now.Millisecond) + " " + message, args);
                this.LogWriter.Flush();
            } catch (Exception exception) {
                // No point blowing up the app just because we can't log.
                Debug.WriteLine(exception.Message);
            }
        }
    }
}