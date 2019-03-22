using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace Utility {
    /// <summary>
    /// Utility methods for getting handles and stuffs
    /// </summary>
    public static class Win32 {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref WinPos lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct WinPos {
            public readonly int Left;
            public readonly int Top;
            public readonly int Right;
            public readonly int Bottom;
        }

        /// <summary>
        /// Checks whether the current topmost window has the provided title
        /// </summary>
        public static bool IsTopmost(string windowTitle) {
            var buff = new StringBuilder(256);
            var handle = GetForegroundWindow();

            return GetWindowText(handle, buff, 256) > 0 && buff.ToString().Equals(windowTitle);
        }

        /// <summary>
        /// Checks if current application is running with administrator permissions. Needed to sever TCP connections.
        /// </summary>
        public static bool CheckElevation() {
            using (var identity = WindowsIdentity.GetCurrent()) {
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}