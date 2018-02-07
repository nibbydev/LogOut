using System;
using System.Drawing;

namespace LogOut {
    class HealthManager {
        public static Bitmap fullHealthBitMap;
        public static Size screenShotSize;
        public static double health;

        public HealthManager () {
            screenShotSize = new Size();
        }

        /// <summary>
        /// Capture a 1 pixel wide centered vertical screenshot of the health globe
        /// </summary>
        /// <returns>The captured image</returns>
        public static Bitmap GetHealthImage() {
            Bitmap screenshot = new Bitmap(Settings.healthWidth, Settings.area_size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics screenGraph = Graphics.FromImage(screenshot);
            screenGraph.CopyFromScreen(Settings.area_left, Settings.area_top, 0, 0, screenShotSize, CopyPixelOperation.SourceCopy);

            // Can save the screenshot but there's no need
            //screenshot.Save("Screenshot.png", Imaging.ImageFormat.Png);

            return screenshot;
        }

        /// <summary>
        /// Gets remaining health as percentage
        /// </summary>
        /// <returns>Remaining health as percentage</returns>
        public static double GetHealthAsPercentage() {
            if (fullHealthBitMap == null || Settings.area_size < 1) return -1;

            // Get screenshot of current health globe
            Bitmap currentHealthBitMap = GetHealthImage();

            int change = 0;
            // Compare all the pixels of the current map versus the full health one
            for (int i = 0; i < Settings.area_size; i++) {
                for (int j = 0; j < Settings.healthWidth; j++) {
                    if (fullHealthBitMap.GetPixel(j, i).R != currentHealthBitMap.GetPixel(j, i).R)
                        change++;
                }
                
            }

            // TODO: find a better solution
            double temp = Settings.area_size;

            // Return the percentage of health remaining
            return 100 - (change / Settings.healthWidth) / temp * 100.0;
        }

        /// <summary>
        /// Periodically calculates health percentage
        /// </summary>
        public static void PollHealth_Task() {
            double lastHealth = 0;
            bool lastNotBelowLimit = true;

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

                // Debugging, I guess?
                if (health > Settings.healthLimitPercent) Console.WriteLine("[HealthManager] Found change: " + health);

                // Do action when health is below limit
                if (health < Settings.healthLimitPercent) {
                    if (lastNotBelowLimit) {
                        // Raise flag so this is not spammed
                        lastNotBelowLimit = false;
                        Console.WriteLine("[HealthManager] Health below limit");
                        
                        // Quit game if event is enabled in settings
                        if (Settings.doLogout) {
                            KillTCP.KillTCPConnectionForProcess();
                            Console.WriteLine("Sending disconnect signal");
                        }
                        
                        // Play sound?
                        System.Media.SystemSounds.Beep.Play();
                        continue;
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
            fullHealthBitMap = GetHealthImage();
        }
    }
}
