using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LogOut {
    /// <summary>
    /// Interaction logic for HealthBarWindow.xaml
    /// </summary>
    public partial class HealthBarWindow : Window {
        public HealthBarWindow() {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the label and rectangle of the element to whatever value is passed
        /// </summary>
        /// <param name="percentage">0-100</param>
        public void SetPercentage(double percentage) {
            if (percentage < 0) percentage = 0;
            else if (percentage > 100) percentage = 100;

            Rectangle.Width = Width * percentage / 100;
            Label.Content = Math.Round(percentage) + "%";
        }

        /// <summary>
        /// Not in use atm. Will be used to notify whether or not program is working
        /// </summary>
        public void SetColor(Color color) {
            Rectangle.Fill = new SolidColorBrush(color);
        }
    }
}
