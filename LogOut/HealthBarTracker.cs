using System;
using System.Drawing;
using System.Text;

namespace LogOut {
    public class HealthBarTracker {
        private Bitmap img;
        private Graphics gfx;
        private Size size;
        private int barLocalOffset;
        private int[] currentHealthState;

        private static int lastOffset;
        public int offset;

        /// <summary>
        /// Changes image capture position and size
        /// </summary>
        public void SetLocation() {
            size = new Size(Settings.captureWidth, Settings.captureHeight);
            currentHealthState = new int[Settings.barWidth];
            barLocalOffset = (int)Math.Round(Settings.barHeight / 4.0 * 3.0);

            img = new Bitmap(Settings.captureWidth, Settings.captureHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            gfx = Graphics.FromImage(img);
        }

        /// <summary>
        /// Main loop of sorts
        /// </summary>
        public void PollHealth_Task() {
            bool lastNotBelowLimit = true;
            double health, lastHealth = 0;

            // Wait until program has found PoE process
            while (Settings.captureTop < 1 || gfx == null) System.Threading.Thread.Sleep(10);
            
            while (true) {
                try {
                    // Run x times a second
                    System.Threading.Thread.Sleep(Settings.healthPollRateMS);

                    // Don't do any calculations until health tracking has been enabled in settings
                    if (!Settings.trackHealth) continue;
                    // Don't track while game client is being moved
                    if (Settings.dontTrackImMoving) continue;

                    // Take screenshot of health bar
                    gfx.CopyFromScreen(Settings.captureLeft, Settings.captureTop, 0, 0, size, CopyPixelOperation.SourceCopy);

                    ParseHealth();
                    health = GetEHPAsPercentage();

                    // Do nothing if state has not changed
                    if (health == lastHealth) continue;
                    else lastHealth = health;

                    // Manage errorcodes
                    if (health == -1) {
                        MainWindow.Log("[WARN] Too many unreadable pixels", -1);
                        img.Save("Screenshot_2_many_unreadable.png", System.Drawing.Imaging.ImageFormat.Png);
                        continue;
                    } else if (health < 1) {
                        MainWindow.Log(" Health bar not visible", -1);
                        continue;
                    }

                    // If topmost window is not PoE
                    if (!Win32.IsTopmost()) continue;

                    // Update health window
                    if (Settings.healthBarEnabled) {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => MainWindow.healthBar.SetPercentage(health));
                    }

                    // Debugging, I guess?
                    if (health > Settings.healthLimitPercent)
                        MainWindow.Log("[Health] Found change: " + health, -1);

                    // Do action when health is below limit
                    if (health < Settings.healthLimitPercent) {
                        if (lastNotBelowLimit) {
                            // Raise flag so this is not spammed
                            lastNotBelowLimit = false;
                            MainWindow.Log("[Health] Health below limit (" + health + ")", 0);

                            // Quit game if event is enabled in settings
                            if (Settings.doLogout) {
                                MainWindow.Log("[Health] Sending disconnect signal", 0);
                                long delay = KillTCP.KillTCPConnectionForProcess();
                                MainWindow.Log("[Health] Disconnected (took " + delay + "ms)", 0);
                            }
                        }
                    } else {
                        lastNotBelowLimit = true;
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                }
            }
        }

        /// <summary>
        /// Extract pixels from captured image
        /// </summary>
        public void ParseHealth() {
            offset = FindBarOffset();

            // Error code. Unable to find health bar offset
            if (offset < 1) {
                //MainWindow.Log(" Invalid offset: " + offset, -1);
                return;
            }

            // Update healthbar overlay and bar capture positions
            if (offset != lastOffset) {
                lastOffset = offset;

                // Calculate bar top border location
                Settings.barTop = Settings.captureTop + offset - Settings.barHeight;
            }

            // Fill pixel array
            for (int x = 0; x < Settings.barWidth; x++) {
                Color color = img.GetPixel(x, offset - barLocalOffset);
                currentHealthState[x] = FindHealthColorMatch(color);
            }
        }

        /// <summary>
        /// Finds offset of healt hbar
        /// </summary>
        /// <returns>How many px away is bottom border from the top</returns>
        private int FindBarOffset() {
            for (int y = Settings.captureHeight - 1; y > Settings.barHeight; y--) {
                try {
                    Color yColor = img.GetPixel(Settings.barHorizontalOffset, y);
                    int yMatch = FindBorderColorMatch(yColor);
                    if (yMatch == -1) continue;
                } catch (ArgumentOutOfRangeException) {
                    // Can't be bothered to figure out why this **extremelyrarely** throws an exception on window resize
                    continue;
                }

                Color zColor = img.GetPixel(Settings.barHorizontalOffset, y - barLocalOffset);
                int zMatch = FindHealthColorMatch(zColor);
                if (zMatch == -1) continue;

                int count = 0;
                for (int x = Settings.barHorizontalOffset; x < Settings.captureWidth - Settings.barHorizontalOffset * 2; x++) {
                    img.SetPixel(x - 1, y, Color.FromArgb(255, 0, 0));
                    Color xColor = img.GetPixel(x, y);
                    int xMatch = FindBorderColorMatch(xColor);

                    count++;

                    if (xMatch == -1) {
                        count = 0;
                        break;
                    }
                }

                if (count > Settings.barWidth - Settings.barHorizontalOffset * 4) {
                    return y;
                }
            }

            // At this point the health bar was probably missed, return error code
            return -1;
        }

        /// <summary>
        /// Matches extracted pixels to preset colors
        /// </summary>
        /// <param name="color">Color to match</param>
        /// <returns>(See settings for descriptions)</returns>
        public int FindHealthColorMatch(Color color) {
            for (int x = 0; x < Settings.topBar.GetLength(0); x++) {
                if (color.R > Settings.topBar[x, 0, 0] && color.R < Settings.topBar[x, 0, 1]) {
                    if (color.G > Settings.topBar[x, 1, 0] && color.G < Settings.topBar[x, 1, 1]) {
                        if (color.B > Settings.topBar[x, 2, 0] && color.B < Settings.topBar[x, 2, 1]) {
                            return x;
                        }
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Matches extracted pixels to preset colors
        /// </summary>
        /// <param name="color">Color to match</param>
        /// <returns>(See settings for descriptions)</returns>
        public int FindBorderColorMatch(Color color) {
            for (int x = 0; x < Settings.bottomBar.GetLength(0); x++) {
                if (color.R > Settings.bottomBar[x, 0, 0] && color.R < Settings.bottomBar[x, 0, 1]) {
                    if (color.G > Settings.bottomBar[x, 1, 0] && color.G < Settings.bottomBar[x, 1, 1]) {
                        if (color.B > Settings.bottomBar[x, 2, 0] && color.B < Settings.bottomBar[x, 2, 1]) {
                            return x;
                        }
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Gets percentage from extracted pixels
        /// </summary>
        /// <returns>Remaining health as 0-100</returns>
        public double GetEHPAsPercentage() {
            //StringBuilder displayLine = new StringBuilder("|", Settings.barWidth);
            double proL = 0, proE = 0;
            int tot = 0, err = 0;

            // Get Life and ES
            for (int i = 0; i < Settings.barWidth; i++) {
                switch (currentHealthState[i]) {
                    case 0:
                    case 1:
                        proL++;
                        proE++;
                        tot++;
                        //displayLine.Append("#");
                        break;
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        proE++;
                        tot++;
                        //displayLine.Append("=");
                        break;
                    case 6:
                    case 7:
                        proL++;
                        tot++;
                        //displayLine.Append("I");
                        break;
                    case 8:
                        tot++;
                        //displayLine.Append(" ");
                        break;
                    case -1:
                        err++;
                        //displayLine.Append("?");
                        break;
                }
            }

            // Print displayLine to console
            //displayLine.Append("|");
            //Console.WriteLine(displayLine.ToString());

            // If more than a third of the pixels were unreadable, return errorcode
            if (err > Settings.barWidth / 3) return -1;

            // Get percentages of both pools
            double prL = proL / tot * 100;
            double prE = proE / tot * 100;

            // If user didn't specify life/ES ratios, default to showing life %
            if (Settings.total_life + Settings.total_es < 1) return Math.Round(prL, 3);

            // Get weights of both pools
            double eHP = Settings.total_life + Settings.total_es;
            double weL = Settings.total_life / eHP;
            double weE = Settings.total_es / eHP;

            return Math.Round(prL * weL + prE * weE, 3);
        }
    }
}
