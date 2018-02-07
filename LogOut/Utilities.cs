using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace LogOut {
    /// <summary>
    /// Made by /u/Umocrajen. Kills TCP connections based on PID
    /// </summary>
    public sealed class KillTCP {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, TcpTableClass tblClass, uint reserved = 0);

        [DllImport("iphlpapi.dll")]
        private static extern int SetTcpEntry(IntPtr pTcprow);

        [StructLayout(LayoutKind.Sequential)]
        public struct MibTcprowOwnerPid {
            public uint state;
            public uint localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] localPort;
            public uint remoteAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] remotePort;
            public uint owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MibTcptableOwnerPid {
            public uint dwNumEntries;
            private readonly MibTcprowOwnerPid table;
        }

        private enum TcpTableClass {
            TcpTableBasicListener,
            TcpTableBasicConnections,
            TcpTableBasicAll,
            TcpTableOwnerPidListener,
            TcpTableOwnerPidConnections,
            TcpTableOwnerPidAll,
            TcpTableOwnerModuleListener,
            TcpTableOwnerModuleConnections,
            TcpTableOwnerModuleAll
        }

        public static long KillTCPConnectionForProcess() {
            long startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            MibTcprowOwnerPid[] table;
            var afInet = 2;
            var buffSize = 0;
            var ret = GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, afInet, TcpTableClass.TcpTableOwnerPidAll);
            var buffTable = Marshal.AllocHGlobal(buffSize);

            try {
                uint statusCode = GetExtendedTcpTable(buffTable, ref buffSize, true, afInet, TcpTableClass.TcpTableOwnerPidAll);
                if (statusCode != 0) return -1;

                var tab = (MibTcptableOwnerPid)Marshal.PtrToStructure(buffTable, typeof(MibTcptableOwnerPid));
                var rowPtr = (IntPtr)((long)buffTable + Marshal.SizeOf(tab.dwNumEntries));
                table = new MibTcprowOwnerPid[tab.dwNumEntries];

                for (var i = 0; i < tab.dwNumEntries; i++) {
                    var tcpRow = (MibTcprowOwnerPid)Marshal.PtrToStructure(rowPtr, typeof(MibTcprowOwnerPid));
                    table[i] = tcpRow;
                    rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(tcpRow));
                }

            } finally {
                Marshal.FreeHGlobal(buffTable);
            }

            // Kill Path Connection
            var PathConnection = table.FirstOrDefault(t => t.owningPid == Settings.processId);
            PathConnection.state = 12;
            var ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(PathConnection));
            Marshal.StructureToPtr(PathConnection, ptr, false);
            SetTcpEntry(ptr);

            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - startTime;
        }
    }

    /// <summary>
    /// Utility methods for getting handles and stuffs
    /// </summary>
    public sealed class Win32 {
        [DllImport("user32.dll", SetLastError = true)]
        static public extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref WinPos lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct WinPos {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static bool IsTopmost() {
            StringBuilder Buff = new StringBuilder(256);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, 256) > 0) {
                if (Buff.ToString() == Settings.clientWindowTitle) return true;
            }

            return false;
        }

        public static bool CheckElevation() {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
