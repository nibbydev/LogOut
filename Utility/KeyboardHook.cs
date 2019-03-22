using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Utility {
    /// <summary>
    /// Execute actions based on hotkeys
    /// </summary>
    public static class KeyboardHook {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x100;

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr _hookId = IntPtr.Zero;
        public static event EventHandler KeyBoardAction;


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);


        public static void Hook() {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule) {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, Callback, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public static void UnHook() => UnhookWindowsHookEx(_hookId);


        private static IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0 && wParam == (IntPtr) WM_KEYDOWN) {
                KeyBoardAction?.Invoke(Marshal.ReadInt32(lParam), EventArgs.Empty);
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }
}