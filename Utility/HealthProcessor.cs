using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Domain;

namespace Utility {
    /// <summary>
    /// Deals with image and pixel processing
    /// </summary>
    public class HealthProcessor {
        private Bitmap img;
        private Graphics gfx;
        private Size size;
        private int barLocalOffset;
        private int[] currentHealthState;

        private static int _lastOffset;


        private bool trackHealth = true;
        private const int HealthPollRateMs = 250;
        public const double HealthLimitPercent = 30;


        // Defines the absolute capture area
        private const int barHorizontalOffset = 5;

        private static double _totalLife;
        private static double _totalEs;
        private const string ClientWindowTitle = "Path of Exile";

        private static readonly Action HealthBelowLimitAction =
            () => Console.WriteLine("[Health] Sending disconnect signal");

        private static readonly int[,,] TopBar = {
            {{58, 78}, {118, 174}, {66, 91}},
            {{85, 129}, {158, 184}, {99, 164}},
            {{60, 83}, {51, 81}, {49, 79}},
            {{90, 100}, {99, 109}, {93, 103}},
            {{107, 113}, {124, 130}, {116, 122}},
            {{115, 141}, {138, 173}, {131, 163}},
            {{23, 38}, {135, 165}, {40, 54}},
            {{23, 38}, {82, 125}, {25, 45}},
            {{25, 55}, {8, 31}, {5, 23}}
        };


        private static readonly Bar[][] HorizontalBar = {
            new[] {
                new Bar {
                    BarType = BarType.EsLife,
                    BarLevel = BarLevel.First,
                    Color = Color.FromArgb(0x7ab694)
                },
                new Bar {
                    BarType = BarType.EsLife,
                    BarLevel = BarLevel.Second,
                    Color = Color.FromArgb(0x7ab694)
                },
            }
        };

        private static readonly Color[][] HorizontalColors = {
            new[] {
                // TopLifeEdge
                Color.FromArgb(0x3d4136), // life + es top
                Color.FromArgb(0x455048), // life + es bottom
                Color.FromArgb(0x2e2a19), // life no es top
                Color.FromArgb(0x232716) // life no es bottom
            },
            new[] {
                // LifeBar
                Color.FromArgb(0x1e782b),
                Color.FromArgb(0x21902d),
                Color.FromArgb(0x1a5a20)
            },
            new[] {
                // EsLifeBar
                Color.FromArgb(0x7ab893),
                Color.FromArgb(0x379546),
                Color.FromArgb(0x639173)
            }
        };

        private static readonly int[,,] BottomBar = {
            {{31, 46}, {17, 34}, {14, 39}},
            {{31, 45}, {22, 34}, {46, 54}}
        };

        private bool _run = true;


        private readonly PictureBox PictureBox;
        private readonly Form ImgBox;


        private readonly HealthTracker tracker;

        public HealthProcessor(HealthTracker healthTracker) {
            tracker = healthTracker;

            PictureBox = new PictureBox {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.CenterImage
            };

            ImgBox = new Form {
                StartPosition = FormStartPosition.CenterScreen,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Size = new Size(200, 60),
                Controls = {PictureBox}
            };

            new Task(() => ImgBox.ShowDialog()).Start();
        }


        private Size lastPicBoxSize;

        private void UpdatePicBox() {
            const int zoom = 4;

            var zoomed = new Bitmap(img.Size.Width * zoom, img.Size.Height * zoom);
            using (var g = Graphics.FromImage(zoomed)) {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(img, new Rectangle(Point.Empty, zoomed.Size));
            }

            PictureBox.Image?.Dispose();
            PictureBox.Image = zoomed;

            ImgBox.Size = new Size((int) (img.Size.Width * zoom * 1.1), (int) (img.Size.Height * zoom * 1.2));
        }


        /// <summary>
        /// Main loop of sorts
        /// </summary>
        public void Run() {
            // todo: replace with timer
            var lastNotBelowLimit = true;
            var lastHealth = 0;

            // Set initial state
            UpdateCaptureLocation();

            while (_run) {
                try {
                    // Run x times a second
                    Thread.Sleep(HealthPollRateMs);

                    // Don't do any calculations until health tracking has been enabled in settings
                    if (!trackHealth) continue;
                    // Don't track while game client is being moved
                    //if (_tracker.WindowMoving) continue;

                    // Take screenshot of health bar
                    gfx.CopyFromScreen(tracker.CapturePos.Left, tracker.CapturePos.Top, 0, 0, size,
                        CopyPixelOperation.SourceCopy);

                    //DrawBoundaries();
                    ParseHealth();
                    UpdatePicBox();

                    continue;

                    var health = GetEhpAsPercentage();

                    // Do nothing if state has not changed
                    if (health == lastHealth) continue;
                    lastHealth = health;

                    // Manage errors
                    if (health == -1) {
                        Console.WriteLine("[WARN] Too many unreadable pixels");
                        img.Save("Screenshot_too_many_unreadable.png", ImageFormat.Png);
                        continue;
                    }

                    if (health < 1) {
                        Console.WriteLine(" Health bar not visible");
                        continue;
                    }

                    // If topmost window is not PoE
                    if (!Win32.IsTopmost(ClientWindowTitle)) continue;

                    // Debugging, I guess?
                    if (health > HealthLimitPercent)
                        Console.WriteLine("[Health] Found change: " + health);

                    // Do action when health is below limit
                    if (health < HealthLimitPercent) {
                        if (!lastNotBelowLimit) continue;

                        // Raise flag so this is not spammed
                        lastNotBelowLimit = false;
                        Console.WriteLine("[Health] Health below limit (" + health + ")");

                        // Quit game if event is enabled in settings
                        HealthBelowLimitAction?.Invoke();
                    }

                    lastNotBelowLimit = true;
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                }
            }
        }


        /// <summary>
        /// Change screen capture location and size
        /// </summary>
        public void UpdateCaptureLocation() {
            size = new Size(tracker.CapturePos.Width, tracker.CapturePos.Height);
            currentHealthState = new int[tracker.BarPos.Width];
            barLocalOffset = (int) Math.Round(tracker.BarPos.Height / 4.0 * 3.0);

            img = new Bitmap(tracker.CapturePos.Width, tracker.CapturePos.Height, PixelFormat.Format32bppArgb);
            gfx = Graphics.FromImage(img);
            gfx.InterpolationMode = InterpolationMode.NearestNeighbor;
        }


        /// <summary>
        /// Extract pixels from captured image
        /// </summary>
        public void ParseHealth() {
            var offset = FindBarLocation();
            return;

            // Error code. Unable to find health bar offset
            if (offset < 1) {
                //MainWindow.Log(" Invalid offset: " + offset, -1);
                return;
            }

            // Update healthbar overlay and bar capture positions
            if (offset != _lastOffset) {
                _lastOffset = offset;

                // Calculate bar top border location
                tracker.BarPos.Top = tracker.CapturePos.Top + offset - tracker.BarPos.Height;
            }

            // Fill pixel array
            for (var x = 0; x < tracker.BarPos.Width; x++) {
                var color = img.GetPixel(x, offset - barLocalOffset);
                currentHealthState[x] = FindHealthColorMatch(color);
            }
        }

        /// <summary>
        /// Finds offset of healthbar
        /// </summary>
        /// <returns>How many px away is bottom border from the top</returns>
        private int FindBarLocation() {
            const int xPixelEdgeOffset = 5;
            var xCenter = tracker.CapturePos.Width / 2;

            // Travel all pixel from top to bottom
            for (var y = 0; y < tracker.CapturePos.Height; y++) {
                // Mark travelled path
                if (y > 0) img.SetPixel(xCenter, y - 1, Color.FromArgb(249, 253, 255));

                // Scan current horizontal pixel line
                var xMatches = HorizontalBarScan(img, y, 95.5, out var similarityPercent);
                
                // Assign color codes
                var similarityColor = (int) Math.Floor(255 * similarityPercent / 100f);
                var mostCommonColor = xMatches.Aggregate((i, j) => i.Count > j.Count ? i : j);

                // Write color codes
                img.SetPixel(tracker.CapturePos.Width - 1, y, mostCommonColor.Color);
                img.SetPixel(tracker.CapturePos.Width - 2, y, Color.FromArgb(similarityColor, 0, 0));
                
                
                
                

                /*if (y < tracker.CapturePos.Height - 5) 
                    img.SetPixel(xCenter, y + 5, Color.FromArgb(133, 255, 52));

                var zColor = img.GetPixel(xPixelEdgeOffset, y + barLocalOffset);
                var zMatch = FindHealthColorMatch(zColor);
                if (zMatch == -1) continue;

                var count = 0;
                for (var x = 0; x < tracker.CapturePos.Width; x++) {
                    if (x > 0) img.SetPixel(x - 1, y, Color.FromArgb(255, 0, 0));

                    var xColor = img.GetPixel(x, y);
                    var xMatch = FindBorderColorMatch(xColor);

                    count++;

                    if (xMatch == -1) {
                        count = 0;
                        break;
                    }
                }

                if (count > tracker.BarPos.Width - xPixelEdgeOffset * 4) return y;*/
            }

            // At this point the health bar was probably missed, return error code
            return -1;
        }

        private static ColorMatch[] HorizontalBarScan(Bitmap img, int y, double threshold, out double similarityPercent) {
            // Get all pixels from the specified row
            var pixels = new Color[img.Width];
            for (var x = 0; x < pixels.Length; x++) {
                pixels[x] = img.GetPixel(x, y);
            }

            var matches = new List<ColorMatch> {
                new ColorMatch {
                    Color = pixels[0]
                }
            };

            for (var i = 1; i < pixels.Length; i++) {
                if (ColorDiff(pixels[i], matches.Last().Color) > threshold) {
                    matches[matches.Count - 1].Count++;
                } else {
                    matches.Add(new ColorMatch {
                        Color = pixels[i]
                    });
                }
            }

            similarityPercent = matches.Max(t => t.Count) / (double) pixels.Length * 100f;
            return matches.ToArray();
        }


        private static void PrintMatrix(int[][] matrix) {
            for (var i = 0; i < matrix.Length; i++) {
                for (var j = 0; j < matrix.Length; j++) {
                    Console.Write(matrix[i][j] + " ");
                }

                Console.WriteLine();
            }

            Console.WriteLine("\n\n");
        }


        private static double ColorDiff(Color e1, Color e2) {
            var rMean = (e1.R + e2.R) / 2;

            var r = e1.R - e2.R;
            var g = e1.G - e2.G;
            var b = e1.B - e2.B;

            return 100 - Math.Sqrt((((512 + rMean) * r * r) >> 8) + 4 * g * g + (((767 - rMean) * b * b) >> 8));
        }

        /// <summary>
        /// Finds offset of healthbar
        /// </summary>
        /// <returns>How many px away is bottom border from the top</returns>
        private int FindBarOffset2() {
            for (var y = tracker.CapturePos.Height - 1; y > tracker.BarPos.Height; y--) {
                try {
                    var yColor = img.GetPixel(barHorizontalOffset, y);
                    var yMatch = FindBorderColorMatch(yColor);
                    if (yMatch == -1) continue;
                } catch (ArgumentOutOfRangeException ex) {
                    // Can't be bothered to figure out why this rarely throws an exception on window resize
                    Console.WriteLine(ex);
                    continue;
                } catch (InvalidOperationException ex) {
                    // Occurs rarely while img is being reassigned via SetLocation()
                    Console.WriteLine(ex);
                    continue;
                }

                var zColor = img.GetPixel(barHorizontalOffset, y - barLocalOffset);
                var zMatch = FindHealthColorMatch(zColor);
                if (zMatch == -1) continue;

                var count = 0;
                for (var x = barHorizontalOffset; x < tracker.CapturePos.Width - barHorizontalOffset * 2; x++) {
                    img.SetPixel(x - 1, y, Color.FromArgb(255, 0, 0));
                    var xColor = img.GetPixel(x, y);
                    var xMatch = FindBorderColorMatch(xColor);

                    count++;

                    if (xMatch == -1) {
                        count = 0;
                        break;
                    }
                }

                if (count > tracker.BarPos.Width - barHorizontalOffset * 4) {
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
        private static int FindHealthColorMatch(Color color) {
            for (var x = 0; x < TopBar.GetLength(0); x++) {
                if (color.R <= TopBar[x, 0, 0] || color.R >= TopBar[x, 0, 1]) continue;
                if (color.G <= TopBar[x, 1, 0] || color.G >= TopBar[x, 1, 1]) continue;
                if (color.B <= TopBar[x, 2, 0] || color.B >= TopBar[x, 2, 1]) continue;

                return x;
            }

            return -1;
        }

        /// <summary>
        /// Matches extracted pixels to preset colors
        /// </summary>
        /// <param name="color">Color to match</param>
        /// <returns>(See settings for descriptions)</returns>
        private static int FindBorderColorMatch(Color color) {
            for (var x = 0; x < BottomBar.GetLength(0); x++) {
                if (color.R <= BottomBar[x, 0, 0] || color.R >= BottomBar[x, 0, 1]) continue;
                if (color.G <= BottomBar[x, 1, 0] || color.G >= BottomBar[x, 1, 1]) continue;
                if (color.B <= BottomBar[x, 2, 0] || color.B >= BottomBar[x, 2, 1]) continue;

                return x;
            }

            return -1;
        }

        /// <summary>
        /// Gets percentage from extracted pixels
        /// </summary>
        /// <returns>Remaining health as 0-100</returns>
        private int GetEhpAsPercentage() {
            //StringBuilder displayLine = new StringBuilder("|", Settings.barWidth);
            double proL = 0, proE = 0;
            int tot = 0, err = 0;

            // Get Life and ES
            for (var i = 0; i < tracker.BarPos.Width; i++) {
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

            // If more than a third of the pixels were unreadable, return error
            if (err > tracker.BarPos.Width / 3) return -1;

            // Get percentages of both pools
            var prL = proL / tot * 100;
            var prE = proE / tot * 100;

            // If user didn't specify life/ES ratios, default to showing life %
            if (_totalLife + _totalEs < 1) {
                return (int) Math.Round(prL);
            }

            // Get weights of both pools
            var eHp = _totalLife + _totalEs;
            var weL = _totalLife / eHp;
            var weE = _totalEs / eHp;

            return (int) Math.Round(prL * weL + prE * weE);
        }
    }
}