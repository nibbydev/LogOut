using System;

namespace LogOut {
    /// <summary>
    /// As I am lazy, this is a class that contains static config values accessible from anywhere in the program
    /// </summary>
    static class Settings {
        // Settings flags
        public static volatile bool workMinimized = false;
        public static volatile bool trackHealth = false;
        public static volatile bool doLogout = false;

        // KeyboardHook
        public static volatile bool saveKey = false;
        public static int logOutHotKey;

        // Settings ranges
        public const int healthPollRate_Min = 0;
        public const int healthPollRate_Max = 1000;
        public const int healthLimit_Min = 0;
        public const int healthLimit_Max = 100;

        // MainWindow
        public const string clientWindowTitle = "Path of Exile";
        public const string programWindowTitle = "TCP Disconnect v1.0";
        public static uint processId;

        // HealthManager
        public static int healthPollRateMS = 10;
        public static double healthLimitPercent = 30;
        public static int area_size;
        public static int area_top;
        public static int area_left;
        public const int healthWidth = 1;

        // Delays
        public const int findGameTaskDelayMS = 1000;
        public const int positionOverlayTaskDelayMS = 500;
    }
}
