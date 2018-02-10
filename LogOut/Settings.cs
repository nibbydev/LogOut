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
        public static volatile bool debugMode = false;

        // KeyboardHook
        public static volatile bool saveKey = false;
        public static int logOutHotKey;

        // Settings ranges
        public const int healthPollRate_Min = 0;
        public const int healthPollRate_Max = 1000;
        public const int healthLimit_Min = 0;
        public const int healthLimit_Max = 100;
        public const int healthWidth_Min = 0;
        public const int healthWidth_Max = 10;

        // MainWindow
        public const string clientWindowTitle = "Path of Exile";
        public const string programWindowTitle = "TCP Disconnect v1.3";
        public static uint processId;

        // Health bar window
        public const int healthBarWidthPercent = 30;
        public static volatile bool healthBarEnabled = false;

        // HealthManager
        public static int healthWidth = 5;
        public static int healthPollRateMS = 10;
        public static double healthLimitPercent = 30;
        public static int area_size;
        public static int area_top;
        public static int area_left;

        // Delays
        public const int findGameTaskDelayMS = 1000;
        public const int positionOverlayTaskDelayMS = 500;
    }
}
