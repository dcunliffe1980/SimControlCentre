using System;
using System.IO;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Diagnostic logger specifically for GoXLR connection and performance issues
    /// </summary>
    public static class GoXLRDiagnostics
    {
        private static readonly object _fileLock = new object();
        private static bool _isEnabled = false;
        private static string? _logDirectory;

        public static void Initialize(string logDirectory, bool enabled)
        {
            _logDirectory = logDirectory;
            _isEnabled = enabled;

            if (_isEnabled)
            {
                Log("INFO", "GoXLR Diagnostics", "Diagnostic logging enabled");
            }
        }

        public static void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (_isEnabled)
            {
                Log("INFO", "GoXLR Diagnostics", "Diagnostic logging enabled");
            }
        }

        public static void Log(string level, string source, string message)
        {
            if (!_isEnabled || string.IsNullOrEmpty(_logDirectory))
                return;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] [{level}] [{source}] {message}";

                // Also write to console for immediate feedback
                Console.WriteLine(logMessage);

                // Write to file
                lock (_fileLock)
                {
                    var logFile = Path.Combine(_logDirectory, $"goxlr_diagnostics_{DateTime.Now:yyyy-MM-dd}.log");
                    File.AppendAllText(logFile, logMessage + Environment.NewLine);
                }
            }
            catch
            {
                // Silently fail - don't break app if logging fails
            }
        }

        public static void Info(string source, string message) => Log("INFO", source, message);
        public static void Warning(string source, string message) => Log("WARN", source, message);
        public static void Error(string source, string message) => Log("ERROR", source, message);
        public static void Debug(string source, string message) => Log("DEBUG", source, message);
    }
}
