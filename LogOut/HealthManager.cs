using System;
using System.Drawing;

namespace LogOut {
    class HealthManager {
        public static Bitmap fullHealthBitMap;
        public static Size screenShotSize;
        public static double health;

        private static Bitmap currentHealthBitMap;
        private static Graphics currentScreenGraph;

        public HealthManager () {
            screenShotSize = new Size();
        }

        /// <summary>
        /// Gets remaining health as percentage
        /// </summary>
        /// <returns>Remaining health as percentage</returns>
        public static double GetHealthAsPercentage() {
            // Take a screenshot of the health bar and save it to currentHealthBitMap
            currentScreenGraph.CopyFromScreen(Settings.area_left, Settings.area_top, 0, 0, screenShotSize, CopyPixelOperation.SourceCopy);

            // Can save the screenshot that was taken but there's no need
            //currentHealthBitMap.Save("Screenshot.png", System.Drawing.Imaging.ImageFormat.Png);

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
                
            // Return the percentage of health remaining
            return 100 - (double)change / Settings.healthWidth / Settings.area_size * 100;
        }

        /// <summary>
        /// Periodically calculates health percentage
        /// </summary>
        public static void PollHealth_Task() {
            bool lastNotBelowLimit = true;
            double lastHealth = 0;

            // Init some things
            currentHealthBitMap = new Bitmap(Settings.healthWidth, Settings.area_size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            currentScreenGraph = Graphics.FromImage(currentHealthBitMap);

            while (true) {
                // Run x times a second
                System.Threading.Thread.Sleep(Settings.healthPollRateMS);

                // Don't do any calculations until this has been enabled
                if (!Settings.trackHealth) continue;
                // If the full health state was cleared, don't compare
                if (fullHealthBitMap == null) continue; 
                // If the PoE window has not been found yet
                if (Settings.area_size < 1) continue;

                // Get current health state
                health = GetHealthAsPercentage();

                // Do nothing if state has not changed
                if (health == lastHealth) continue;
                else lastHealth = health;

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
            Bitmap temp_currentHealthBitMap = new Bitmap(Settings.healthWidth, Settings.area_size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics temp_screenGraph = Graphics.FromImage(temp_currentHealthBitMap);

            // Take a screenshot of the health bar and save it to currentHealthBitMap
            temp_screenGraph.CopyFromScreen(Settings.area_left, Settings.area_top, 0, 0, screenShotSize, CopyPixelOperation.SourceCopy);

            // Write the currentHealthBitMap to fullHealthBitMap and presume that's what 100% health looks like
            fullHealthBitMap = temp_currentHealthBitMap;
        }
    }
}
