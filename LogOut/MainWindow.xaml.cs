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
        private HealthOverlayWindow healthOverlayWindow;
        private SettingsWindow settingsWindow;

        public static Win32.WinPos lastWinPos;
        public static IntPtr client_hWnd;
        public static TextBox console;

        private Task findGame_Task;
        private Task pollHealth_Task;
        private Task positionHealthOverlay_Task;

        /// <summary>
        /// Initializes elements
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            // Assign console box to static variable
            console = TextBox_Console;

            // Set window title
            Title = Settings.programWindowTitle;

            // Print credentials
            Log(Settings.programWindowTitle + " by Siegrest", 0);

            // Warn user on no admin rights
            if (!Win32.CheckElevation()) Log("Elevated access required for disconnect", 1);

            // Run task to find application handle. Runs until game handle is found
            findGame_Task = Task.Run(() => FindGame_Task());

            // Run task to find application coordinates. Runs until program exits
            positionHealthOverlay_Task = new Task(() => PositionHealthOverlay_Task());

            // Define new instances of settings window and health overlay window
            healthOverlayWindow = new HealthOverlayWindow();
            settingsWindow = new SettingsWindow();
        }

        /// <summary>
        /// Taks that gets application's handler and PID
        /// Runs until those are found
        /// Starts the positionHealthOverlay_Task task
        /// </summary>
        private void FindGame_Task() {
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
                        Dispatcher.Invoke(() => Log("Waiting for PoE process...", 0));
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

            // Invoke dispatcher, allowing UI element updates
            Dispatcher.Invoke(() => {
                // Enable hotkey button
                Button_SetHotkey.IsEnabled = true;

                // If runOnce was lowered that means when the app was launched PoE was not running
                // and the message "Waiting for PoE process..." was displayed. So, naturally we need
                // to inform the user that the processs has been found now
                if (!runOnce) Log("PoE process found", 0);
            });

            // Now that the game handle has been found, start a task that calculates the health globe's
            // position
            positionHealthOverlay_Task.Start();
        }

        /// <summary>
        /// Task that's run in the background from launch until program exit
        /// Finds coordinates of game's window and does area calculations
        /// Reacts to window position/size changes
        /// </summary>
        private void PositionHealthOverlay_Task() {
            while (true) {
                Win32.WinPos winPos = new Win32.WinPos();
                Win32.GetWindowRect(client_hWnd, ref winPos);

                // Recalculate 2 times a second
                // Don't recalculate if there has been no change in window size/position
                if (lastWinPos.Equals(winPos)) {
                    System.Threading.Thread.Sleep(Settings.positionOverlayTaskDelayMS);
                    continue;
                } else lastWinPos = winPos; // Save latest window position

                // Null the saved health state as the window moved and it's no longer valid
                if (HealthManager.fullHealthBitMap != null) {
                    HealthManager.fullHealthBitMap = null;
                    Dispatcher.Invoke(() => Log("Window moved. Saved health no longer valid", 2));
                    System.Media.SystemSounds.Beep.Play();
                }

                // Calculate HealthOverlay position (quick mafs)
                Settings.area_size = (winPos.Bottom - winPos.Top) * 18 / 100;
                Settings.area_top = winPos.Bottom - Settings.area_size - Settings.area_size * 10 / 100;
                Settings.area_left = winPos.Left + Settings.area_size / 2 + Settings.area_size * 14 / 100;
                HealthManager.screenShotSize.Height = HealthManager.screenShotSize.Width = Settings.area_size;

                // Update HealthOverlay position
                Dispatcher.Invoke(() => SetHealthOverlayPos());

                // Just for some debugging
                Console.WriteLine("[HealthOverlay] Window position change: " +
                    Settings.area_left + "x " + Settings.area_top + "y " + Settings.area_size + "size");

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

            // Close other windows on exit
            healthOverlayWindow.Close();
            settingsWindow.Close();

            // Close app (not sure if the above close methods are even needed)
            System.Windows.Application.Current.Shutdown();
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
        /// Updates the location of the health overlay
        /// </summary>
        private void SetHealthOverlayPos() {
            healthOverlayWindow.Top = Settings.area_top;
            healthOverlayWindow.Left = Settings.area_left - Settings.area_size / 2;
            healthOverlayWindow.Width = healthOverlayWindow.Height = Settings.area_size;
        }

        /// <summary>
        /// Opens the Settings panel as dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Settings_Click(object sender, RoutedEventArgs e) {
            settingsWindow.ShowDialog();
        }

        /// <summary>
        /// Displays a button above the health globe
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_SaveHealth_Click(object sender, RoutedEventArgs e) {
            if (!Settings.trackHealth) {
                Log("Health tracking disabled in settings", 2);
                return;
            }

            Log("Waiting for button press..", 0);
            healthOverlayWindow.ShowDialog();
            Log("Saved health globe's current position and values", 0);

            // Run task to find changes in health
            if (pollHealth_Task == null) pollHealth_Task = Task.Run(() => HealthManager.PollHealth_Task());
        }
    }
}
