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
            TextBox_HealthWidth.Text = Settings.healthWidth.ToString();

            // Enable/disable controls
            TextBox_HealthLimit.IsEnabled = Settings.trackHealth;
            TextBox_PollRate.IsEnabled = Settings.trackHealth;
            TextBox_HealthWidth.IsEnabled = Settings.trackHealth;
            CheckBox_AutoAction.IsChecked = Settings.trackHealth;
            CheckBox_Logout.IsEnabled = Settings.trackHealth;
            CheckBox_Debug.IsChecked = Settings.debugMode;

            // Set tooltips
            TextBox_PollRate.ToolTip = "Range: " + Settings.healthPollRate_Min + " - " + Settings.healthPollRate_Max + " (milliseconds)";
            TextBox_HealthLimit.ToolTip = "Range: " + Settings.healthLimit_Min + " - " + Settings.healthLimit_Max + " (percent)";
            TextBox_HealthWidth.ToolTip = "Range: " + Settings.healthWidth_Min + " - " + Settings.healthWidth_Max + " (pixels)";
        }

        private void Button_Apply_Click(object sender, RoutedEventArgs e) {
            int.TryParse(TextBox_PollRate.Text, out int rate);
            if (rate != Settings.healthPollRateMS) {
                if (rate > Settings.healthPollRate_Min && rate <= Settings.healthPollRate_Max) {
                    MainWindow.Log("[Settings][Rate] " + Settings.healthPollRateMS + " -> " + rate, -1);
                    Settings.healthPollRateMS = rate;
                } else {
                    TextBox_PollRate.Text = Settings.healthPollRateMS.ToString();
                    MainWindow.Log("[Settings][Rate] Error applying value " + rate, -1);
                }
            }

            int.TryParse(TextBox_HealthLimit.Text, out int limit);
            if (limit != Settings.healthLimitPercent) {
                if (limit > Settings.healthLimit_Min && limit <= Settings.healthLimit_Max) {
                    MainWindow.Log("[Settings][Limit] " + Settings.healthLimitPercent + " -> " + limit, -1);
                    Settings.healthLimitPercent = limit;
                } else {
                    TextBox_HealthLimit.Text = Settings.healthLimitPercent.ToString();
                    MainWindow.Log("[Settings][Limit] Error applying value " + limit, -1);
                }
            }

            int.TryParse(TextBox_HealthWidth.Text, out int width);
            if (width != Settings.healthWidth) {
                if (width > Settings.healthWidth_Min && width <= Settings.healthWidth_Max) {
                    MainWindow.Log("[Settings][Width] " + Settings.healthWidth + " -> " + width, -1);
                    Settings.healthWidth = width;
                } else {
                    TextBox_HealthWidth.Text = Settings.healthWidth.ToString();
                    MainWindow.Log("[Settings][Width] Error applying value " + width, -1);
                }
            }

            Settings.workMinimized = (bool)CheckBox_Minimized.IsChecked;
            Settings.trackHealth = (bool)CheckBox_AutoAction.IsChecked;
            Settings.debugMode = (bool)CheckBox_Debug.IsChecked;
            Settings.doLogout = (bool)CheckBox_Logout.IsChecked;

            Hide();
        }

        // Keep
        private void CheckBox_AutoAction_Click(object sender, RoutedEventArgs e) {
            // Enable/disable controls
            TextBox_HealthLimit.IsEnabled = (bool)CheckBox_AutoAction.IsChecked;
            TextBox_HealthWidth.IsEnabled = (bool)CheckBox_AutoAction.IsChecked;
            TextBox_PollRate.IsEnabled = (bool)CheckBox_AutoAction.IsChecked;
            CheckBox_Logout.IsEnabled = (bool)CheckBox_AutoAction.IsChecked;
        }

        /// <summary>
        /// Instead of closing the window, hide it. Don't save settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            e.Cancel = true;
            Hide();

            // Enable/disable controls
            TextBox_HealthLimit.IsEnabled = Settings.trackHealth;
            TextBox_PollRate.IsEnabled = Settings.trackHealth;
            TextBox_HealthWidth.IsEnabled = Settings.trackHealth;
            CheckBox_AutoAction.IsChecked = Settings.trackHealth;
            CheckBox_Logout.IsEnabled = Settings.trackHealth;
            CheckBox_Debug.IsChecked = Settings.debugMode;

            // Revert settings to original state
            CheckBox_Minimized.IsChecked = Settings.workMinimized;
            CheckBox_AutoAction.IsChecked = Settings.trackHealth;
            CheckBox_Debug.IsChecked = Settings.debugMode;
            CheckBox_Logout.IsChecked = Settings.doLogout;

            // Revert textbox values
            TextBox_PollRate.Text = Settings.healthPollRateMS.ToString();
            TextBox_HealthLimit.Text = Settings.healthLimitPercent.ToString();
            TextBox_HealthWidth.Text = Settings.healthWidth.ToString();
        }
    }
}
