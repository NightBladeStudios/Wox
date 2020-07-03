namespace Wox.Infrastructure
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    public static class ShellCommand
    {
        public delegate bool EnumThreadDelegate(IntPtr hwnd, IntPtr lParam);

        private static bool containsSecurityWindow;

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(uint threadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hwnd);

        #region Public

        public static Process RunAsDifferentUser(ProcessStartInfo processStartInfo)
        {
            processStartInfo.Verb = "RunAsUser";
            var process = Process.Start(processStartInfo);

            containsSecurityWindow = false;
            while (!containsSecurityWindow) // wait for windows to bring up the "Windows Security" dialog
            {
                CheckSecurityWindow();
                Thread.Sleep(25);
            }

            while (containsSecurityWindow) // while this process contains a "Windows Security" dialog, stay open
            {
                containsSecurityWindow = false;
                CheckSecurityWindow();
                Thread.Sleep(50);
            }

            return process;
        }

        public static ProcessStartInfo SetProcessStartInfo(this string fileName, string workingDirectory = "", string arguments = "", string verb = "")
        {
            var info = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                Arguments = arguments,
                Verb = verb
            };

            return info;
        }

        #endregion

        #region Private

        private static void CheckSecurityWindow()
        {
            var ptc = Process.GetCurrentProcess().Threads;
            for (var i = 0; i < ptc.Count; i++)
                EnumThreadWindows((uint) ptc[i].Id, CheckSecurityThread, IntPtr.Zero);
        }

        private static bool CheckSecurityThread(IntPtr hwnd, IntPtr lParam)
        {
            if (GetWindowTitle(hwnd) == "Windows Security")
                containsSecurityWindow = true;
            return true;
        }

        private static string GetWindowTitle(IntPtr hwnd)
        {
            var sb = new StringBuilder(GetWindowTextLength(hwnd) + 1);
            GetWindowText(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }

        #endregion
    }
}