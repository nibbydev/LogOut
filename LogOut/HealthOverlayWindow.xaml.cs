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
    /// Interaction logic for HealthBox.xaml
    /// </summary>
    public partial class HealthOverlayWindow : Window {
        public HealthOverlayWindow() {
            InitializeComponent();
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e) {
            Hide();
            HealthManager.SaveFullHealthState();
        }
    }
}
