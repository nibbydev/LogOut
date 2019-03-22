using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Utility {
    /// <summary>
    /// Originally by /u/Umocrajen. Kills TCP connections based on PID.
    /// </summary>
    public static class TcpSever {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion,
            TcpTableClass tblClass, uint reserved = 0);

        [DllImport("iphlpapi.dll")]
        private static extern int SetTcpEntry(IntPtr pTcprow);

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcprowOwnerPid {
            public uint state;
            public uint localAddr;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] localPort;

            public uint remoteAddr;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] remotePort;

            public uint owningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcptableOwnerPid {
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

        public static long SeverTcp(uint pid) {
            var startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            var buffSize = 0;
            MibTcprowOwnerPid[] table;
            var buffTable = Marshal.AllocHGlobal(buffSize);

            try {
                var statusCode = GetExtendedTcpTable(buffTable, ref buffSize, true, 2,
                    TcpTableClass.TcpTableOwnerPidAll);
                if (statusCode != 0) return -1;

                var tab = (MibTcptableOwnerPid) Marshal.PtrToStructure(buffTable, typeof(MibTcptableOwnerPid));
                var rowPtr = (IntPtr) ((long) buffTable + Marshal.SizeOf(tab.dwNumEntries));
                table = new MibTcprowOwnerPid[tab.dwNumEntries];

                for (var i = 0; i < tab.dwNumEntries; i++) {
                    var tcpRow = (MibTcprowOwnerPid) Marshal.PtrToStructure(rowPtr, typeof(MibTcprowOwnerPid));
                    table[i] = tcpRow;
                    rowPtr = (IntPtr) ((long) rowPtr + Marshal.SizeOf(tcpRow));
                }
            } finally {
                Marshal.FreeHGlobal(buffTable);
            }

            var connection = table.FirstOrDefault(t => t.owningPid == pid);
            connection.state = 12;

            var ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(connection));
            Marshal.StructureToPtr(connection, ptr, false);
            SetTcpEntry(ptr);

            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - startTime;
        }
    }
}