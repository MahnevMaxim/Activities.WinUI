using System;
using System.IO;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace WinUIExplorer
{
    /// <summary>Логгер приложения WinUIExplorer (NLog, файловый target).</summary>
    public static class Logger
    {
        private static string s_logFilePath;

        /// <summary>Полный путь к файлу лога.</summary>
        public static string FilePath => s_logFilePath ?? BuildLogFilePath();

        static Logger()
        {
            try
            {
                s_logFilePath = BuildLogFilePath();
                if (File.Exists(s_logFilePath)) { try { File.Delete(s_logFilePath); } catch { } }

                var config = new LoggingConfiguration();
                var fileTarget = new FileTarget("file")
                {
                    FileName = s_logFilePath,
                    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${exception:format=ToString}",
                    KeepFileOpen = false,
                    ConcurrentWrites = true,
                    Encoding = Encoding.UTF8
                };
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, fileTarget);
                LogManager.Configuration = config;
            }
            catch { }
        }

        public static void Info(string message) => Write(LogLevel.Info, message);

        public static void Warn(string message) => Write(LogLevel.Warn, message);

        public static void Error(string message) => Write(LogLevel.Error, message);

        public static void Error(string message, Exception ex)
        {
            try { LogManager.GetLogger("WinUIExplorer").Error(ex, message ?? string.Empty); } catch { }
        }

        private static void Write(LogLevel level, string message)
        {
            try { LogManager.GetLogger("WinUIExplorer").Log(level, message ?? string.Empty); } catch { }
        }

        private static string BuildLogFilePath()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(root))
                root = Path.GetTempPath();

            string dir = Path.Combine(root, "Activities.WinUI");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "WinUIExplorer.log");
        }
    }
}
