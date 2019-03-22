using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Utility {
    /// <summary>
    /// Allows hooking to win events (eg window move/resize/minimize)
    /// </summary>
    public class WinEventHook {
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject,
            int idChild, uint dwEventThread, uint dwmsEventTime);

        private IntPtr windowEventHook;

        private const int WINEVENT_INCONTEXT = 0x0004;
        private const int WINEVENT_OUTOFCONTEXT = 0x0000;
        private const int WINEVENT_SKIPOWNPROCESS = 0x0002;
        private const int WINEVENT_SKIPOWNTHREAD = 0x0001;
        
        public const int EVENT_SYSTEM_MOVESIZESTART = 0x000A;
        public const int EVENT_SYSTEM_MOVESIZEEND = 0x000B;
        public const int EVENT_SYSTEM_FOREGROUND = 0x0003;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);


        /// <summary>
        /// Create the hook
        /// </summary>
        public void Hook(WinEventDelegate procDel, uint pid, uint eventCode, uint? eventEndCode = null) {
            if (windowEventHook != IntPtr.Zero) {
                return;
            }

            windowEventHook = eventEndCode == null
                ? SetWinEventHook(eventCode, eventCode, IntPtr.Zero, procDel, pid, 0, WINEVENT_OUTOFCONTEXT)
                : SetWinEventHook(eventCode, (uint) eventEndCode, IntPtr.Zero, procDel, pid, 0, WINEVENT_OUTOFCONTEXT);

            // Unable to hook
            if (windowEventHook == IntPtr.Zero) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        /// Close the hook
        /// </summary>
        public void UnHook() {
            if (windowEventHook == IntPtr.Zero) {
                return;
            }
            
            // Attempt to unhook
            if (!UnhookWinEvent(windowEventHook)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }
}