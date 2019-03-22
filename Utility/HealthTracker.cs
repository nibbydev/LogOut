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

            var gameWidth = winPos.Right - winPos.Left;
            var gameHeight = winPos.Bottom - winPos.Top;

            // Calculate health bar size
            BarPos.Width = (int) Math.Round(gameHeight * 9.6 / 100.0);
            BarPos.Height = (int) Math.Round(gameHeight * 1.7 / 100.0);
            BarPos.Left = (int) Math.Round(winPos.Left + gameWidth / 2.0 - BarPos.Width / 2.0);

            // Some measurements are shared
            CapturePos.Width = BarPos.Width;
            CapturePos.Left = BarPos.Left;

            // Calculate capture area
            CapturePos.Top = (int) Math.Round(winPos.Top + gameHeight * 26 / 100.0);
            CapturePos.Height = (int) Math.Round(gameHeight / 2.0 - gameHeight * 40 / 100.0);

            // Update positions
            updateTrackerCaptureLocation.Invoke();
            Console.WriteLine("poof");
        }
    }
}