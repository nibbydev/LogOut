using System;
using System.Diagnostics;
using System.Threading;
using Domain;

namespace Utility {
    /// <summary>
    /// Deals with discovering the game window and finding coordinates for the health bar
    /// </summary>
    public class HealthTracker {
        private string TargetWindowTitle = "Path of Exile";
        private const int FindGameTaskDelayMs = 1000;

        private IntPtr gameHWnd;
        private uint? processId;

        public Pos CapturePos { get; } = new Pos();
        public Pos BarPos { get; } = new Pos();


        public bool WindowMoving { get; private set; } // is the window currently being moved/dragged?
        private Win32.WinPos lastWinPos;

        public Action updateTrackerCaptureLocation;
        private Timer updateTimer;


        public void OverWriteGameHandle(string handle) {
            TargetWindowTitle = handle;
        }
        

        /// <summary>
        /// Gets game client's handle and PID
        /// </summary>
        public void FindGameHandle() {
            // todo: replace with system events
            var runOnce = true;

            // Run until game proc found
            while (true) {
                // Check all current running processes
                foreach (var proc in Process.GetProcesses()) {
                    if (!proc.MainWindowTitle.Equals(TargetWindowTitle)) {
                        continue;
                    }
                    
                    gameHWnd = proc.MainWindowHandle;
                    processId = (uint) proc.Id;
                    break;
                }

                // Target application found
                if (processId != null) {
                    break;
                }

                if (runOnce) {
                    Console.WriteLine("Waiting for process..");
                    runOnce = false;
                }

                Thread.Sleep(FindGameTaskDelayMs);
            }

            Console.WriteLine("Process found");

            TimerCallback();
            var span = TimeSpan.FromMilliseconds(1000);
            updateTimer = new Timer(TimerCallback, null, span, span);
        }

        /// <summary>
        /// When game client is moved or resized, recalculates overlay element positions
        /// </summary>
        private void TimerCallback(object state = null) {
            Console.Write(".");

            WindowMoving = !Win32.IsTopmost(TargetWindowTitle);

            var winPos = new Win32.WinPos();
            Win32.GetWindowRect(gameHWnd, ref winPos);

            if (lastWinPos.Equals(winPos)) return;
            lastWinPos = winPos;
            
            // Window height scale multiplier for the health globe
            const double globeWhsm = 4.80f;
            const double captureOffset = 0.935f;

            // Window size correction
            const int borderOffset = 16;
            const int titleBarOffset = 39;

            var gameWidth = winPos.Right - winPos.Left - borderOffset;
            var gameHeight = winPos.Bottom - winPos.Top - titleBarOffset;

            var captureSize = (int) Math.Floor(gameHeight / globeWhsm);
            var captureOffsetPixels = (int) Math.Floor(captureSize * (1 - captureOffset));

            // Position capture area over health globe
            CapturePos.Left = winPos.Left + borderOffset / 2 + captureOffsetPixels;
            CapturePos.Top = winPos.Bottom - borderOffset / 2 - captureSize + captureOffsetPixels;
            
            CapturePos.Width = captureSize - captureOffsetPixels;
            CapturePos.Height = captureSize - captureOffsetPixels;

            // Update positions
            updateTrackerCaptureLocation.Invoke();
            Console.WriteLine("poof");
        }
    }
}