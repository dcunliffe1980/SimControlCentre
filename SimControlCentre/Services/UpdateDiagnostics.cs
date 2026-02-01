using System;
using System.IO;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Diagnostic logger for update checker
    /// </summary>
    public static class UpdateDiagnostics
    {
        private static readonly object _fileLock = new object();
        private static string? _logDirectory;

        public static void Initialize(string logDirectory)
        {
            _logDirectory = logDirectory;
        }

        public static void Log(string message)
        {
            if (string.IsNullOrEmpty(_logDirectory))
                return;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] {message}";

                // Write to console for immediate feedback
                Console.WriteLine(logMessage);

                // Write to file
                lock (_fileLock)
                {
                    var logFile = Path.Combine(_logDirectory, $"update_checker_{DateTime.Now:yyyy-MM-dd}.log");
                    File.AppendAllText(logFile, logMessage + Environment.NewLine);
                }
            }
            catch
            {
                // Silently fail - don't break app if logging fails
            }
        }
    }
}
