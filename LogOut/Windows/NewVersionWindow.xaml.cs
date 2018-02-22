using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;

namespace LogOut {
    /// <summary>
    /// Interaction logic for NewVersionWindow.xaml
    /// </summary>
    public partial class NewVersionWindow : Window {
        private WebClient webClient;

        public NewVersionWindow() {
            InitializeComponent();

            webClient = new WebClient();
            webClient.Headers.Add("user-agent", "This can't be null so here's a string");

            Task.Run(() => Run());
        }

        /// <summary>
        /// Downloads list of releases from Github API and returns latest
        /// </summary>
        /// <returns>Latest release object</returns>
        private ReleaseObject GetLatestRelease() {
            try {
                string jsonString = webClient.DownloadString("https://api.github.com/repos/siegrest/LogOut/releases");
                List<ReleaseObject> tempList = new JavaScriptSerializer().Deserialize<List<ReleaseObject>>(jsonString);

                if (tempList == null) return null;
                else return tempList[0];
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }

            return null;
        }

        /// <summary>
        /// Get latest release and show updater window if version is newer
        /// </summary>
        private void Run() {
            ReleaseObject latest = GetLatestRelease();
            if (latest == null) {
                return;
            } else if (latest.tag_name == Settings.programVersion) {
                return;
            };

            Dispatcher.Invoke(() => {
                Label_NewVersion.Content = latest.tag_name;
                Label_CurrentVersion.Content = Settings.programVersion;
                HyperLink_URL.NavigateUri = new Uri(latest.html_url);

                ShowDialog();
            });
        }

        /// <summary>
        /// Opens up the webbrowser when URL is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HyperLink_URL_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }

    sealed class ReleaseObject {
        public string html_url { get; set; } 
        public string tag_name { get; set; }
        public string name { get; set; }
    }
}
