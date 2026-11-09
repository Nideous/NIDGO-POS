using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace NIDGO_POS
{
    public static class Logger
    {
        private static readonly string LogFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NIDGO_POS",
            "Logs"
        );

        static Logger()
        {
            try
            {
                if (!Directory.Exists(LogFolder))
                {
                    Directory.CreateDirectory(LogFolder);
                }
                CleanOldLogs(keepDays: 14); // Rotate logs older than 14 days
            }
            catch { /* Fail-safe initialization */ }
        }

        public static void LogException(Exception ex, string context = "Unhandled Exception")
        {
            try
            {
                string logFile = Path.Combine(LogFolder, $"pos_log_{DateTime.Now:yyyyMMdd}.log");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("==================================================");
                sb.AppendLine($"Timestamp : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Context   : {context}");
                sb.AppendLine($"Message   : {ex.Message}");
                sb.AppendLine($"Type      : {ex.GetType().FullName}");
                sb.AppendLine($"StackTrace:\n{ex.StackTrace}");
                sb.AppendLine();

                File.AppendAllText(logFile, sb.ToString(), Encoding.UTF8);
            }
            catch { /* Fail-safe fallback */ }
        }

        public static void HandleGlobalException(Exception ex, string threadName)
        {
            LogException(ex, $"Global Unhandled Exception ({threadName})");

            MessageBox.Show(
                "An unexpected application error occurred.\n\n" +
                "The error details have been recorded securely in the system diagnostic log for support analysis.\n\n" +
                "If this error persists, please contact your System Administrator.",
                "System Operational Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        private static void CleanOldLogs(int keepDays)
        {
            DirectoryInfo di = new DirectoryInfo(LogFolder);
            DateTime cutoff = DateTime.Now.AddDays(-keepDays);

            foreach (FileInfo file in di.GetFiles("pos_log_*.log"))
            {
                if (file.CreationTime < cutoff)
                {
                    try { file.Delete(); } catch { }
                }
            }
        }
    }
}