using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LogOut {
    /// <summary>
    /// Interaction logic for HealthBarWindow.xaml
    /// </summary>
    public partial class HealthBarWindow : Window {
        public HealthBarWindow() {
            InitializeComponent();
            Height = 30;
        }

        /// <summary>
        /// Sets the label and rectangle of the element to whatever value is passed
        /// </summary>
        /// <param name="percentage">0-100</param>
        public void SetPercentage(double percentage) {
            if (percentage < 0) percentage = 0;
            else if (percentage > 100) percentage = 100;

            ProgressBar_Main.Value = percentage;
        }
    }
}
