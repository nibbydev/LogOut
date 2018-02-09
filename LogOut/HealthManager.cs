using System;
using System.Drawing;

namespace LogOut {
    class HealthManager {
        public static Bitmap fullHealthBitMap;
        public static Size screenShotSize;
        public static double health;

        private static Bitmap currentHealthBitMap;
        private static Graphics screenGraph;

        public HealthManager () {
            screenShotSize = new Size();
        }

        /// <summary>
        /// Capture a x pixel wide centered vertical screenshot of the health globe
        /// Saves it to currentHealthBitMap
        /// </summary>
        /// <returns>The captured image</returns>
        public static void GetHealthImage() {
            screenGraph.CopyFromScreen(Settings.area_left, Settings.area_top, 0, 0, screenShotSize, CopyPixelOperation.SourceCopy);

            // Can save the screenshot that was taken but there's no need
            //currentHealthBitMap.Save("Screenshot.png", Imaging.ImageFormat.Png);
        }

        /// <summary>
        /// Gets remaining health as percentage
        /// </summary>
        /// <returns>Remaining health as percentage</returns>
        public static double GetHealthAsPercentage() {
            if (fullHealthBitMap == null || Settings.area_size < 1) return -1;

            // Take a screenshot of the health bar and save it to currentHealthBitMap
            GetHealthImage();

            int change = 0;
            // Compare all the pixels of the current map versus the full health one
            // And by all I mean just the red ones. 
            for (int i = 0; i < Settings.area_size; i++) {
                for (int j = 0; j < Settings.healthWidth; j++) {
                    if (fullHealthBitMap.GetPixel(j, i).R != currentHealthBitMap.GetPixel(j, i).R) {
                        change++;
                    }
                }
            }
                
            // TODO: find a better solution
            double temp = Settings.area_size;

            // Return the percentage of health remaining
            return 100 - (change / Settings.healthWidth) / temp * 100;
        }

        /// <summary>
        /// Periodically calculates health percentage
        /// </summary>
        public static void PollHealth_Task() {
            bool lastNotBelowLimit = true;
            double lastHealth = 0;

            while (true) {
                // Run x times a second
                System.Threading.Thread.Sleep(Settings.healthPollRateMS);

                // Don't do any calculations until this has been enabled
                if (!Settings.trackHealth) continue;

                // Get current health state
                health = GetHealthAsPercentage();

                // Do nothing if state has not changed
                if (health == lastHealth) continue;
                lastHealth = health;

                // Error code handling
                if (health == -1) {
                    Console.WriteLine("[HealthManager] Not initialized (" + health + ")");
                    continue;
                } else if (health < 1) {
                    Console.WriteLine("Major change: 100%. Loading screen?");
                    continue;
                }

                // If topmost window is not PoE
                if (!Win32.IsTopmost()) {
                    Console.WriteLine("[HealthManager][NOT TOPMOST] Found change (" + health + ")");
                    continue;
                }

                // If last saved window position does not equal current window position,
                // which means the game window was moved or resized, then remove last saved
                // health status
                Win32.WinPos winPos = new Win32.WinPos();
                Win32.GetWindowRect(MainWindow.client_hWnd, ref winPos);
                if (!MainWindow.lastWinPos.Equals(winPos)) {
                    MainWindow.Log("Window moved. Saved health cleared", 3);
                    System.Media.SystemSounds.Beep.Play();
                    fullHealthBitMap = null;
                    continue;
                }

                // Debugging, I guess?
                if (health > Settings.healthLimitPercent) Console.WriteLine("[HealthManager] Found change: " + health);

                // Do action when health is below limit
                if (health < Settings.healthLimitPercent) {
                    if (lastNotBelowLimit) {
                        // Raise flag so this is not spammed
                        lastNotBelowLimit = false;
                        MainWindow.Log("Health below limit", 0);

                        // Quit game if event is enabled in settings
                        if (Settings.doLogout) {
                            MainWindow.Log("Sending disconnect signal", 0);
                            long delay = KillTCP.KillTCPConnectionForProcess();
                            MainWindow.Log("Disconnected (took " + delay + "ms)", 0);
                        }
                    }
                } else {
                    lastNotBelowLimit = true;
                }
            }
        }

        /// <summary>
        /// Saves the current health state as full
        /// </summary>
        public static void SaveFullHealthState() {
            // Create a new instance of Bitmap and Graphics (the size might have changed)
            currentHealthBitMap = new Bitmap(Settings.healthWidth, Settings.area_size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            screenGraph = Graphics.FromImage(currentHealthBitMap);

            // Take a screenshot of the health bar and save it to currentHealthBitMap
            GetHealthImage();

            // Write the currentHealthBitMap to fullHealthBitMap and presume that's what 100% health looks like
            fullHealthBitMap = new Bitmap(currentHealthBitMap);
        }
    }
}
