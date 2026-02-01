using System;
using System.IO;
using System.Threading;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Simple file-based logger for debugging
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDirectory;
        private static readonly string LogFilePath;
        private static readonly object LogLock = new object();
        private static bool _isInitialized = false;

        static Logger()
        {
            // Log to: %LocalAppData%\SimControlCentre\logs\
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            LogDirectory = Path.Combine(appDataPath, "SimControlCentre", "logs");
            
            // Create log file with timestamp: SimControlCentre_2024-01-31.log
            var fileName = $"SimControlCentre_{DateTime.Now:yyyy-MM-dd}.log";
            LogFilePath = Path.Combine(LogDirectory, fileName);
            
            Initialize();
        }

        private static void Initialize()
        {
            try
            {
                // Create logs directory if it doesn't exist
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                // Write startup header
                lock (LogLock)
                {
                    File.AppendAllText(LogFilePath, 
                        $"\n========================================\n" +
                        $"SimControlCentre Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                        $"========================================\n\n");
                }

                _isInitialized = true;
                
                // Clean up old logs (keep last 7 days)
                CleanOldLogs(7);
            }
            catch (Exception ex)
            {
                // If logging fails, write to console as fallback
                Console.WriteLine($"[Logger] Failed to initialize: {ex.Message}");
            }
        }

        private static void CleanOldLogs(int daysToKeep)
        {
            try
            {
                var files = Directory.GetFiles(LogDirectory, "SimControlCentre_*.log");
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // Ignore errors in cleanup
            }
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        public static void Info(string component, string message)
        {
            Log("INFO", component, message);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void Warning(string component, string message)
        {
            Log("WARN", component, message);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void Error(string component, string message, Exception? ex = null)
        {
            var fullMessage = ex != null 
                ? $"{message}\nException: {ex.Message}\nStack trace: {ex.StackTrace}" 
                : message;
            Log("ERROR", component, fullMessage);
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public static void Debug(string component, string message)
        {
            Log("DEBUG", component, message);
        }

        private static void Log(string level, string component, string message)
        {
            if (!_isInitialized)
                return;

            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var threadId = Thread.CurrentThread.ManagedThreadId;
                var logLine = $"[{timestamp}] [{level,-5}] [{component,-20}] [T{threadId}] {message}\n";

                lock (LogLock)
                {
                    File.AppendAllText(LogFilePath, logLine);
                }

                // Also write to console for development
                Console.WriteLine(logLine.TrimEnd());
            }
            catch
            {
                // If logging fails, silently ignore to prevent crashing the app
            }
        }

        /// <summary>
        /// Get the current log file path
        /// </summary>
        public static string GetLogFilePath()
        {
            return LogFilePath;
        }

        /// <summary>
        /// Get the logs directory path
        /// </summary>
        public static string GetLogsDirectory()
        {
            return LogDirectory;
        }

        /// <summary>
        /// Open the logs folder in Windows Explorer
        /// </summary>
        public static void OpenLogsFolder()
        {
            try
            {
                if (Directory.Exists(LogDirectory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", LogDirectory);
                }
            }
            catch (Exception ex)
            {
                Error("Logger", "Failed to open logs folder", ex);
            }
        }
    }
}
