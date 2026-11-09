using System;
using System.Threading;
using System.Windows.Forms;

namespace NIDGO_POS
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // =========================================================================
            // 3-LEVEL ERROR HANDLING: GLOBAL UNHANDLED EXCEPTION REGISTRATION
            // =========================================================================

            // 1. Catch UI Thread Exceptions
            Application.ThreadException += (sender, e) =>
                Logger.HandleGlobalException(e.Exception, "UI Thread");

            // 2. Catch Non-UI Thread / AppDomain Exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Logger.HandleGlobalException(ex, "Domain Non-UI Thread");
                }
            };

            // 3. Force WinForms to funnel all unhandled exceptions through ThreadException
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Launch Application starting at Login Window
            Application.Run(new LoginForm());
        }
    }
}