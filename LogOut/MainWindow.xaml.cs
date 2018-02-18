using System;
using System.Diagnostics;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LogOut {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private EventHandler keyboardEventHandler;
        private SettingsWindow settingsWindow;
        public static HealthBarTracker tracker;
        public static Win32.WinPos lastWinPos;
        public static IntPtr client_hWnd;
        public static TextBox console;

        private Task findGameHandle_Task;
        private Task pollHealth_Task;
        private Task positionOverlay_Task;

        public static HealthBarWindow healthBar;
        public static HealthOverlayWindow healthOverlay;

        /// <summary>
        /// Initializes elements
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            // Init instances
            healthBar = new HealthBarWindow();
            tracker = new HealthBarTracker();
            settingsWindow = new SettingsWindow();
            healthOverlay = new HealthOverlayWindow();

            // Assign console box to static variable
            console = TextBox_Console;

            // Set window title
            Title = Settings.programWindowTitle;

            // Print credentials
            Log(Settings.programWindowTitle + " by Siegrest", 0);

            // Warn user on no admin rights
            Settings.elevatedAccess = Win32.CheckElevation();
            if (!Settings.elevatedAccess) Log("Elevated access required for disconnect", 1);

            // Run tasks
            findGameHandle_Task = Task.Run(() => FindGameHandle_Task());
            positionOverlay_Task = Task.Run(() => PositionHealthOverlay_Task());
            pollHealth_Task = Task.Run(() => tracker.PollHealth_Task());
        }

        /// <summary>
        /// Taks that gets application's handler and PID
        /// Runs until those are found
        /// Starts the positionHealthOverlay_Task task
        /// </summary>
        private void FindGameHandle_Task() {
            // Flag that allows us to print messages like "Waiting for PoE process..."
            // only if the game is not running
            bool runOnce = true; 

            // Run every x ms and attempt to find game client
            while (true) {
                // Get process handler from name
                foreach (Process proc in Process.GetProcesses())
                    if (proc.MainWindowTitle == Settings.clientWindowTitle)
                        client_hWnd = proc.MainWindowHandle;

                // If PoE is not running
                if (client_hWnd == IntPtr.Zero) {
                    // If first run print text
                    if (runOnce) {
                        runOnce = false;
                        Log("Waiting for PoE process...", 0);
                    }

                    System.Threading.Thread.Sleep(Settings.findGameTaskDelayMS);
                    continue;
                }

                // Get window PID from handler
                Win32.GetWindowThreadProcessId(client_hWnd, out Settings.processId);
                // Not 100% sure if needed but I'll keep it here just to be safe
                if (Settings.processId <= 0) continue;

                break;
            }

            // Enable hotkey button
            if (Settings.elevatedAccess) Dispatcher.Invoke(() => Button_SetHotkey.IsEnabled = true);

            // If runOnce was lowered that means when the app was launched PoE was not running
            // and the message "Waiting for PoE process..." was displayed. So, naturally we need
            // to inform the user that the processs has been found now
            if (!runOnce) Log("PoE process found", 0);
        }

        /// <summary>
        /// Task that's run in the background from launch until program exit
        /// Finds coordinates of game's window and does area calculations
        /// Reacts to window position/size changes
        /// </summary>
        private void PositionHealthOverlay_Task() {
            // Wait until game handle is found
            while (Settings.processId <= 0) System.Threading.Thread.Sleep(10);

            while (true) {
                Win32.WinPos winPos = new Win32.WinPos();
                Win32.GetWindowRect(client_hWnd, ref winPos);

                // Recalculate x times a second
                // Don't recalculate if there has been no change in window size/position
                if (lastWinPos.Equals(winPos)) {
                    System.Threading.Thread.Sleep(Settings.positionOverlayTaskDelayMS);
                    continue;
                } else lastWinPos = winPos; // Save latest window position

                int width = winPos.Right - winPos.Left;
                int height = winPos.Bottom - winPos.Top;
                double half_width = width / 2.0;

                // Calculate position for the healthbar that's above the char's head
                Settings.width = (int)(height * 9.6 / 100.0);
                Settings.height = (int)(height * 1.7 / 100.0);
                Settings.left = (int)(winPos.Left + half_width - Settings.width / 2.0);
                Settings.top = (int)(winPos.Top + height * 30.9 / 100.0 + Settings.height);

                // Position UI elements
                Dispatcher.Invoke(() => {
                    // Position big healthbar
                    healthBar.Width = width * Settings.healthBarWidthPercent / 100.0;
                    healthBar.Left = winPos.Left + half_width - healthBar.Width / 2.0;
                    healthBar.Top = winPos.Top + 50;

                    // Position healthbar overlay
                    tracker.SetLocation();
                });

                // Pause briefly until checking for changes again
                System.Threading.Thread.Sleep(Settings.positionOverlayTaskDelayMS);
            }
        }

        /// <summary>
        /// Keyboard event handler. Fires when the hotkey is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Keyboard(object sender, EventArgs e) {
            Log("[KEY] Captured hotkey", -1);

            if (Settings.saveKey) {
                Settings.saveKey = false;
                Button_SetHotkey.IsEnabled = true;
                Log("Assigned TCP disconnect to key: " + (System.Windows.Forms.Keys)Settings.logOutHotKey, 0);
                return;
            }

            // Don't send disconnect if game is minimized and checkbox is not ticked
            if (Settings.workMinimized && !Win32.IsTopmost()) return;

            // Send disconnect signal
            Log("Closing TCP connections...", 0);
            long delay = KillTCP.KillTCPConnectionForProcess();
            Log("Closed connections (took " + delay + " ms)", 0);
        }

        /// <summary>
        /// Event handler that closes hooks on program exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            KeyboardHook.KeyBoardAction -= keyboardEventHandler;
            KeyboardHook.Stop();

            // Close app
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Event handler that flips a flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_SetHotkey_Click(object sender, RoutedEventArgs e) {
            Log("Press any key...", 0);
            Button_SetHotkey.IsEnabled = false;
            // Raise flag, indicating next key that will be pressed is gonna serve as a hotkey
            Settings.saveKey = true;

            // Hook keyboard. This only runs once
            if (Settings.logOutHotKey == 0) {
                keyboardEventHandler = new EventHandler(Event_Keyboard);
                KeyboardHook.KeyBoardAction += keyboardEventHandler;
                KeyboardHook.Start();
            }
        }

        /// <summary>
        /// Timestamp and prefix local console messages
        /// </summary>
        /// <param name="str"></param>
        /// <param name="status"></param>
        public static void Log(string str, int status) {
            string prefix;

            switch (status) {
                default:
                case -1:
                    // -1 stands for debug due to poor planning. Don't display debug messages
                    // if the setting has not been enabled
                    if (!Settings.debugMode) return;
                    prefix = "[DEBUG]";
                    break;
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
            Application.Current.Dispatcher.Invoke(() => {
                console.AppendText("[" + time + "]" + prefix + str + "\n");
                console.ScrollToEnd();
            });
        }

        /// <summary>
        /// Opens the Settings panel as dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Settings_Click(object sender, RoutedEventArgs e) {
            settingsWindow.ShowDialog();
        }
    }
}
