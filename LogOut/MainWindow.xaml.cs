using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Utility;
using Application = System.Windows.Application;
using TextBox = System.Windows.Controls.TextBox;

namespace LogOut {
    public partial class MainWindow : Window {
        private EventHandler keyboardEventHandler;
        private readonly SettingsWindow settingsWindow;
        private static HealthProcessor _tracker;
        private static IntPtr _clientHWnd;
        private static TextBox _console;
        private Win32.WinPos lastWinPos;

        private Task taskFindGameHandle;
        private Task taskPollHealth;
        private Task taskPositionOverlay;

        public static HealthBarWindow healthBar;
        public static HealthOverlayWindow healthOverlay;
        public static UpdateWindow newVersion;

        public static WinEventHook moveSizeEvent;
        public static WinEventHook foregroundEvent;

        /// <summary>
        /// Initializes elements
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            _console = TextBox_Console;
            Title = $"{Settings.programTitle} {Settings.programVersion}";
            Log(Title, 0);

            // Warn user on no admin rights
            Settings.elevatedAccess = Win32.CheckElevation();
            if (!Settings.elevatedAccess) Log("Elevated access required for disconnect", 1);

            // Init instances
            healthBar = new HealthBarWindow();
            //_tracker = new HealthBarProcessor();
            settingsWindow = new SettingsWindow();
            healthOverlay = new HealthOverlayWindow();
            newVersion = new UpdateWindow();

            // Run tasks
            taskFindGameHandle = Task.Run(() => FindGameHandle_Task());
            taskPollHealth = Task.Run(() => _tracker.Run());
            taskPositionOverlay = Task.Run(() => PositionHealthOverlay_Task());

            // Define hooks
            moveSizeEvent = new WinEventHook();
            foregroundEvent = new WinEventHook();
        }

        /// <summary>
        /// Applies various system hooks via SetWinEventHook
        /// </summary>
        private void HookHooks() {
            moveSizeEvent.Hook(EventCallback, Settings.processId, Settings.EVENT_SYSTEM_MOVESIZESTART, Settings.EVENT_SYSTEM_MOVESIZEEND);
            foregroundEvent.Hook(EventCallback, 0, Settings.EVENT_SYSTEM_FOREGROUND);
            foregroundEvent.Hook(EventCallback, 0, Settings.EVENT_SYSTEM_FOREGROUND);
        }

        /// <summary>
        /// When game client is moved or resized, recalculates overlay element positions
        /// </summary>
        private void EventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            if (eventType > 0) Log("[winEvent] Event (" + eventType + "): " + hwnd, -1);

            // Foreground window change event
            if (eventType == Settings.EVENT_SYSTEM_FOREGROUND) {
                // When another application gets foreground focus, pause the program and hides UI elements
                if (Win32.IsTopmost(Settings.clientWindowTitle)) {
                    Settings.dontTrackImMoving = false;
                    if (Settings.healthBarEnabled && !healthBar.IsVisible) healthBar.Show();
                    if (Settings.showCaptureOverlay && !healthOverlay.IsVisible) healthOverlay.Show();
                } else {
                    Settings.dontTrackImMoving = true;
                    if (Settings.healthBarEnabled && healthBar.IsVisible) healthBar.Hide();
                    if (Settings.showCaptureOverlay && healthOverlay.IsVisible) healthOverlay.Hide();
                }
            }

            // Flip pause flag during window move
            if (eventType == Settings.EVENT_SYSTEM_MOVESIZESTART) {
                Settings.dontTrackImMoving = true;
                if (Settings.healthBarEnabled && healthBar.IsVisible) healthBar.Hide();
                if (Settings.showCaptureOverlay && healthOverlay.IsVisible) healthOverlay.Hide();
                return;
            }

            var winPos = new Win32.WinPos();
            Win32.GetWindowRect(_clientHWnd, ref winPos);

            if (lastWinPos.Equals(winPos)) return;
            lastWinPos = winPos;

            var gameWidth = winPos.Right - winPos.Left;
            var gameHeight = winPos.Bottom - winPos.Top;

            // Calculate health bar size
            Settings.barWidth = (int)Math.Round(gameHeight * 9.6 / 100.0);
            Settings.barHeight = (int)Math.Round(gameHeight * 1.7 / 100.0);
            Settings.barLeft = (int)Math.Round(winPos.Left + gameWidth / 2.0 - Settings.barWidth / 2.0);

            // Some measurements are shared
            Settings.captureWidth = Settings.barWidth;
            Settings.captureLeft = Settings.barLeft;

            // Calculate capture area
            Settings.captureTop = (int)Math.Round(winPos.Top + gameHeight * 20 / 100.0);
            Settings.captureHeight = (int)Math.Round(gameHeight / 2.0 - gameHeight * 25 / 100.0);

            // Position UI elements
            Dispatcher.Invoke(() => {
                // Position big health bar
                healthBar.Width = (int)Math.Round(gameWidth * 30.0 / 100.0);
                healthBar.Height = (int)Math.Round(gameHeight * 5.0 / 100.0);
                healthBar.Left = (int)Math.Round(winPos.Left + gameWidth / 2.0 - healthBar.Width / 2.0);
                healthBar.Top = (int)Math.Round(winPos.Top + gameHeight * 5.0 / 100.0);

                // Position health bar tracker
                _tracker.UpdateCaptureLocation();

                // Position health bar overlay
                healthOverlay.Left = Settings.captureLeft;
                healthOverlay.Top = Settings.captureTop;
                healthOverlay.Width = Settings.captureWidth;
                healthOverlay.Height = Settings.captureHeight;
            });

            if (eventType == Settings.EVENT_SYSTEM_MOVESIZEEND) {
                Settings.dontTrackImMoving = false;
                if (Settings.healthBarEnabled && !healthBar.IsVisible) healthBar.Show();
                if (Settings.showCaptureOverlay && !healthOverlay.IsVisible) healthOverlay.Show();
            }
        }

        /// <summary>
        /// Gets game client's handler and PID
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
                        _clientHWnd = proc.MainWindowHandle;

                // If PoE is not running
                if (_clientHWnd == IntPtr.Zero) {
                    // If first run print text
                    if (runOnce) {
                        runOnce = false;
                        Log("Waiting for PoE process...", 0);
                    }

                    Thread.Sleep(Settings.findGameTaskDelayMS);
                    continue;
                }

                // Get window PID from handler
                Win32.GetWindowThreadProcessId(_clientHWnd, out Settings.processId);
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

            // Hook events
            Dispatcher.Invoke(() => HookHooks());
            // Run the callback method using dummy variables
            EventCallback(IntPtr.Zero, 0, IntPtr.Zero, 0, 0, 0, 0);
        }

        /// <summary>
        /// Periodically checks the game client's position
        /// </summary>
        private void PositionHealthOverlay_Task() {
            while (true) {
                Thread.Sleep(Settings.positionOverlayTaskDelayMS);

                // Wait until game handle is found
                if (Settings.processId <= 0) continue;

                // Run the callback method using dummy variables
                EventCallback(IntPtr.Zero, 0, IntPtr.Zero, 0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Keyboard event handler. Fires when the hotkey is pressed
        /// </summary>
        private void Event_Keyboard(object sender, EventArgs e) {
            Log("[KEY] Captured hotkey", -1);

            if (Settings.saveKey) {
                Settings.saveKey = false;
                Button_SetHotkey.IsEnabled = true;
                Log("Assigned TCP disconnect to key: " + (Keys)Settings.logOutHotKey, 0);
                return;
            }

            // Don't send disconnect if game is minimized and checkbox is not ticked
            if (Settings.workMinimized && !Win32.IsTopmost(Settings.clientWindowTitle)) return;

            // Send disconnect signal
            Log("Closing TCP connections...", 0);
            var delay = TcpSever.SeverTcp(Settings.processId);
            Log("Closed connections (took " + delay + " ms)", 0);
        }

        /// <summary>
        /// Event handler that closes hooks on program exit
        /// </summary>
        private void Window_Closing(object sender, CancelEventArgs e) {
            KeyboardHook.KeyBoardAction -= keyboardEventHandler;
            KeyboardHook.UnHook();

            moveSizeEvent.UnHook(); 
            foregroundEvent.UnHook();

            // Close app
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Event handler that flips a flag
        /// </summary>
        private void Button_SetHotkey_Click(object sender, RoutedEventArgs e) {
            Log("Press any key...", 0);
            Button_SetHotkey.IsEnabled = false;
            // Raise flag, indicating next key that will be pressed is gonna serve as a hotkey
            Settings.saveKey = true;

            // Hook keyboard. This only runs once
            if (Settings.logOutHotKey == 0) {
                keyboardEventHandler = Event_Keyboard;
                KeyboardHook.KeyBoardAction += keyboardEventHandler;
                KeyboardHook.Hook();
            }
        }

        /// <summary>
        /// Timestamp and prefix local console messages
        /// </summary>
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
                    prefix = "[INFO]";
                    break;
                case 1:
                    prefix = "[WARN]";
                    break;
                case 2:
                    prefix = "[ERROR]";
                    break;
                case 3:
                    prefix = "[CRITICAL]";
                    break;
            }

            // Add a space if there's no additional box
            if (str[0] != '[') prefix += " ";

            string time = string.Format("{0:HH:mm:ss}", DateTime.Now);
            Application.Current.Dispatcher.Invoke(() => {
                _console.AppendText("[" + time + "]" + prefix + str + "\n");
                _console.ScrollToEnd();
            });
        }

        /// <summary>
        /// Opens the Settings panel as dialog
        /// </summary>
        private void Button_Settings_Click(object sender, RoutedEventArgs e) {
            settingsWindow.ShowDialog();
        }
    }
}
