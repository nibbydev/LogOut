using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace LogOut {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private EventHandler eventHandler;
        private IntPtr client_hWnd;
        private HealthOverlayWindow healthOverlayWindow;
        private SettingsWindow settingsWindow;
        private Task healthThread;

        /// <summary>
        /// Initialize elements
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            // Set window title
            Title = Settings.programWindowTitle;

            // Print credentials
            Log(Settings.programWindowTitle + " by Siegrest", 0);

            // Hook
            eventHandler = new EventHandler(Event_keyboard);
            KeyboardHook.KeyBoardAction += eventHandler;
            KeyboardHook.Start();

            // Warn user on no admin rights
            if (!Win32.CheckElevation()) Log("Elevated access required for disconnect", 1);

            // Run task to find application handle
            Task.Run(() => FindGameTask());

            // Init HealthOverlay
            healthOverlayWindow = new HealthOverlayWindow();

            // Init settings window
            settingsWindow = new SettingsWindow();
        }

        /// <summary>
        /// Get application's handler and PID
        /// </summary>
        private void FindGameTask() {
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
                        Dispatcher.Invoke(() => { Log("Waiting for PoE process...", 0); });
                        runOnce = false;
                    }

                    System.Threading.Thread.Sleep(Settings.findGameTaskDelayMS);
                    continue;
                }

                // Get window PID from handler
                Win32.GetWindowThreadProcessId(client_hWnd, out Settings.processId);
                if (Settings.processId <= 0) continue;

                break;
            }

            // Invoke dispatcher, allowing UI element updates
            Dispatcher.Invoke(() => {
                Button_SetKey.IsEnabled = true;
                if (!runOnce) Log("PoE process found", 0);
            });

            // Run task to find application coordinates
            Task.Run(() => PositionHealthOverlay_Task());
        }

        /// <summary>
        /// Task run in the background. Finds coordinates of game's window and fits a rectangle over the health bar
        /// </summary>
        private void PositionHealthOverlay_Task() {
            Win32.WinPos lastWinPos = new Win32.WinPos();
            
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
        /// Keyboard event handler. Fires when "registred hotkey" is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_keyboard(object sender, EventArgs e) {
            if (Settings.saveKey) {
                Settings.saveKey = false;
                Button_SetKey.IsEnabled = true;
                Log("Assigned TCP disconnect to key: " + (Keys)Settings.logOutHotKey, 0);
                return;
            }

            // Don't send disconnect if game is minimized and checkbox is not ticked
            if (Settings.workMinimized && !Win32.IsTopmost()) return;

            // Send disconnect signal
            long time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            Log("Closing TCP connections...", 0);
            KillTCP.KillTCPConnectionForProcess();
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

            // Close other windows on exit
            healthOverlayWindow.Close();
            settingsWindow.Close();
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Event handler that flips a flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_SetKey_Click(object sender, RoutedEventArgs e) {
            Log("Press any key...", 0);
            Button_SetKey.IsEnabled = false;
            // Raise flag, indicating next key that will be pressed is gonna serve as a hotkey
            Settings.saveKey = true;
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
            if (healthThread == null) healthThread = Task.Run(() => HealthManager.PollHealth_Task());
        }
    }
}
