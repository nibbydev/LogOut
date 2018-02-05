using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;

namespace LogOut {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private EventHandler eventHandler;
        private IntPtr client_hWnd;
        private uint processId;
        private const string GAME_WINDOW_TITLE = "Path of Exile";
        private const string APP_WINDOW_TITLE = "TCP Disconnect v0.2";

        /// <summary>
        /// Utility methods for getting handles and stuffs
        /// </summary>
        sealed class Win32 {
            [DllImport("user32.dll", SetLastError = true)]
            static public extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

            [DllImport("user32.dll")]
            private static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

            public static string GetTopmostWindowTitle() {
                int nChars = 256;

                System.Text.StringBuilder Buff = new System.Text.StringBuilder(nChars);
                IntPtr handle = GetForegroundWindow();

                if (GetWindowText(handle, Buff, nChars) > 0)
                    return Buff.ToString();
                else
                    return null;
            }

            public static bool checkElevation() {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
        }

        /// <summary>
        /// Made by /u/Umocrajen
        /// </summary>
        sealed class KillTCP {
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

            public static void KillTCPConnectionForProcess(uint ProcessId) {
                MibTcprowOwnerPid[] table;
                var afInet = 2;
                var buffSize = 0;
                var ret = GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, afInet, TcpTableClass.TcpTableOwnerPidAll);
                var buffTable = Marshal.AllocHGlobal(buffSize);

                try {
                    uint statusCode = GetExtendedTcpTable(buffTable, ref buffSize, true, afInet, TcpTableClass.TcpTableOwnerPidAll);
                    if (statusCode != 0) return;

                    var tab = (MibTcptableOwnerPid) Marshal.PtrToStructure(buffTable, typeof(MibTcptableOwnerPid));
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
                var PathConnection = table.FirstOrDefault(t => t.owningPid == ProcessId);
                PathConnection.state = 12;
                var ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(PathConnection));
                Marshal.StructureToPtr(PathConnection, ptr, false);
                SetTcpEntry(ptr);
            }
        }

        /// <summary>
        /// Initialize elements
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            // Set window title
            Title = APP_WINDOW_TITLE;

            // Print credentials
            Log(APP_WINDOW_TITLE + " by Siegrest", 0);

            // Hook
            eventHandler = new EventHandler(Event_keyboard);
            KeyboardHook.KeyBoardAction += eventHandler;
            KeyboardHook.Start();

            // Warn user on no admin rights
            if (!Win32.checkElevation()) Log("Elevated access required for disconnect", 1);

            // Run task to find application handle
            System.Threading.Tasks.Task.Run(() => FindGameTask());
        }

        /// <summary>
        /// Get application's handler and PID
        /// </summary>
        private void FindGameTask() {
            bool runOnce = true;
            // Run every 100ms and attempt to find game client
            while (true) {
                // Get process handler from name
                foreach (Process pList in Process.GetProcesses()) {
                    if (pList.MainWindowTitle == GAME_WINDOW_TITLE)
                        client_hWnd = pList.MainWindowHandle;
                }
                
                // If PoE is not running
                if (client_hWnd == IntPtr.Zero) {
                    // If first run print text
                    if (runOnce) {
                        Dispatcher.Invoke(new Action(() => {
                            Log("Waiting for PoE process...", 0);
                        }));
                        runOnce = false;
                    }

                    System.Threading.Thread.Sleep(1000);
                    continue;
                }

                // Get window PID from handler
                Win32.GetWindowThreadProcessId(client_hWnd, out processId);
                if (processId <= 0) continue;

                break;
            }

            // Invoke dispatcher, allowing UI element updates
            Dispatcher.Invoke(new Action(() => {
                Button_SetKey.IsEnabled = true;
                CheckBox_Minimized.IsEnabled = true;
                if (!runOnce) Log("PoE process found", 0);
            }));
        }

        /// <summary>
        /// Keyboard event handler. Fires when "registred hotkey" is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_keyboard(object sender, EventArgs e) {
            if (KeyboardHook.flag_saveKey) {
                KeyboardHook.flag_saveKey = false;
                Button_SetKey.IsEnabled = true;
                Log("Assigned TCP disconnect to key: " + (Keys)KeyboardHook.KEY, 0);
                return;
            }

            // Don't send disconnect if game is minimized and checkbox is not ticked
            if (!(bool)CheckBox_Minimized.IsChecked) {
                if (Win32.GetTopmostWindowTitle() != GAME_WINDOW_TITLE) return;
            }

            // Send disconnect signal
            long time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            Log("Closing TCP connections...", 0);
            KillTCP.KillTCPConnectionForProcess(processId);
            time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - time;
            Log("Closed connections (took " + time + " ms)", 0);
        }

        /// <summary>
        /// Event handler that closes hooks on program exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            KeyboardHook.KeyBoardAction -= eventHandler;
            KeyboardHook.Stop();
        }

        /// <summary>
        /// Event handler that flips a flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_SetKey_Click(object sender, RoutedEventArgs e) {
            Log("Press any key...", 0);
            Button_SetKey.IsEnabled = false;
            KeyboardHook.flag_saveKey = true;
        }

        /// <summary>
        /// Timestamp and prefix local console messages
        /// </summary>
        /// <param name="str"></param>
        /// <param name="status"></param>
        private void Log(string str, int status) {
            string prefix;

            switch (status) {
                default:
                case 0:
                    prefix = "[INFO] ";
                    break;
                case 1:
                    prefix = "[WARN] ";
                    break;
                case 2:
                    prefix = "[ERROR] ";
                    break;
                case 3:
                    prefix = "[CRITICAL] ";
                    break;
            }

            string time = string.Format("{0:HH:mm:ss}", DateTime.Now);
            TextBox_Console.AppendText("[" + time + "]" + prefix + str + "\n");
            TextBox_Console.ScrollToEnd();
        }
    }
}
