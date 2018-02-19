using System;
using System.Windows;

namespace LogOut {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        public SettingsWindow() {
            InitializeComponent();
            
            // Import initial settings
            TextBox_PollRate.Text = Settings.healthPollRateMS.ToString();
            TextBox_HealthLimit.Text = Settings.healthLimitPercent.ToString();

            // Enable/disable controls
            GroupBox_Health.IsEnabled = Settings.trackHealth;
            CheckBox_Debug.IsChecked = Settings.debugMode;

            // Set tooltips
            TextBox_PollRate.ToolTip = "Range: " + Settings.healthPollRate_Min + " - " + Settings.healthPollRate_Max + " (milliseconds)";
            TextBox_HealthLimit.ToolTip = "Range: " + Settings.healthLimit_Min + " - " + Settings.healthLimit_Max + " (percent)";
        }

        /// <summary>
        /// Apply all settings on button press
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Apply_Click(object sender, RoutedEventArgs e) {
            // Pollrate
            int.TryParse(TextBox_PollRate.Text, out int input);
            if (input != Settings.healthPollRateMS) {
                if (input >= Settings.healthPollRate_Min && input <= Settings.healthPollRate_Max) {
                    MainWindow.Log("[Settings][Rate] " + Settings.healthPollRateMS + " -> " + input, -1);
                    Settings.healthPollRateMS = input;
                } else {
                    TextBox_PollRate.Text = Settings.healthPollRateMS.ToString();
                    MainWindow.Log("[Settings][Rate] Error applying value " + input, -1);
                }
            }

            // Health %
            int.TryParse(TextBox_HealthLimit.Text, out input);
            if (input != Settings.healthLimitPercent) {
                if (input > Settings.healthLimit_Min && input <= Settings.healthLimit_Max) {
                    MainWindow.Log("[Settings][Limit] " + Settings.healthLimitPercent + " -> " + input, -1);
                    Settings.healthLimitPercent = input;
                } else {
                    TextBox_HealthLimit.Text = Settings.healthLimitPercent.ToString();
                    MainWindow.Log("[Settings][Limit] Error applying value " + input, -1);
                }
            }

            // Life
            int.TryParse(TextBox_Life.Text, out input);
            if (input != Settings.total_life) {
                if (input >= 0) Settings.total_life = input;
                else TextBox_Life.Text = Settings.total_life.ToString();
            }

            // ES
            int.TryParse(TextBox_ES.Text, out input);
            if (input != Settings.total_es) {
                if (input >= 0) Settings.total_es = input;
                else TextBox_Life.Text = Settings.total_es.ToString();
            }

            Settings.workMinimized = (bool)CheckBox_Minimized.IsChecked;
            Settings.trackHealth = (bool)CheckBox_Track.IsChecked;
            Settings.debugMode = (bool)CheckBox_Debug.IsChecked;
            Settings.doLogout = (bool)CheckBox_Logout.IsChecked;
            Settings.healthBarEnabled = (bool)CheckBox_ShowHealthBar.IsChecked;
            Settings.showCaptureOverlay = (bool)CheckBox_CaptureArea.IsChecked;

            // Show/hide external UI elements
            Application.Current.Dispatcher.Invoke(() => {
                if (Settings.trackHealth) {
                    if (Settings.healthBarEnabled)  MainWindow.healthBar.Show();
                    else  MainWindow.healthBar.Hide();

                    if (Settings.showCaptureOverlay)  MainWindow.healthOverlay.Show();
                    else  MainWindow.healthOverlay.Hide();
                }
            });

            Hide();
        }

        /// <summary>
        /// Enable controls with master control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Track_Click(object sender, RoutedEventArgs e) {
            GroupBox_Health.IsEnabled = (bool)CheckBox_Track.IsChecked;

            TextBox_Life.Text = Settings.total_life.ToString();
            TextBox_ES.Text = Settings.total_es.ToString();
        }

        /// <summary>
        /// Instead of closing the window, hide it. Don't save settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            e.Cancel = true;
            Hide();
            RevertSettings();
        }

        /// <summary>
        /// Revert settings and hide window on cancel button press
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Cancel_Click(object sender, RoutedEventArgs e) {
            Hide();
            RevertSettings();
        }

        /// <summary>
        /// Revert all settings to previous state
        /// </summary>
        private void RevertSettings() {
            // Disable group controls
            GroupBox_Health.IsEnabled = Settings.trackHealth;

            // Revert checkboxes
            CheckBox_Track.IsChecked = Settings.trackHealth;
            CheckBox_Debug.IsChecked = Settings.debugMode;
            CheckBox_ShowHealthBar.IsChecked = Settings.healthBarEnabled;
            CheckBox_Minimized.IsChecked = Settings.workMinimized;
            CheckBox_Logout.IsChecked = Settings.doLogout;
            CheckBox_CaptureArea.IsChecked = Settings.showCaptureOverlay;

            // Revert textbox values
            TextBox_PollRate.Text = Settings.healthPollRateMS.ToString();
            TextBox_HealthLimit.Text = Settings.healthLimitPercent.ToString();
            TextBox_Life.Text = Settings.total_life.ToString();
            TextBox_ES.Text = Settings.total_es.ToString();
        }

        /// <summary>
        /// Enables/disables "TCP disconnect" checkbox based on admin rights
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            CheckBox_Logout.IsEnabled = Settings.elevatedAccess;
        }
    }
}
