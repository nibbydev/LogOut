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
        public const int healthPollRate_Max = 2000;
        public const int healthLimit_Min = 0;
        public const int healthLimit_Max = 100;
        public const int healthWidth_Min = 0;
        public const int healthWidth_Max = 10;

        // MainWindow
        public const string clientWindowTitle = "Path of Exile";
        public const string programWindowTitle = "PoeLogout v2.2.5";
        public static uint processId;
        public static bool elevatedAccess = false;

        // Health bar window
        public static volatile bool healthBarEnabled = false;

        // HealthManager
        public static int healthPollRateMS = 10;
        public static double healthLimitPercent = 30;

        // Delays
        public const int findGameTaskDelayMS = 1000;
        public const int positionOverlayTaskDelayMS = 100;

        public static readonly int[,,] topBar = {
            { {  58,  78 }, { 118, 174 }, {  66,  91 } },
            { {  85, 129 }, { 158, 184 }, {  99, 164 } },
            { {  60,  83 }, { 51,   81 }, {  49,  79 } },
            { {  90, 100 }, { 99,  109 }, {  93, 103 } },
            { { 107, 113 }, { 124, 130 }, { 116, 122 } },
            { { 115, 141 }, { 138, 173 }, { 131, 163 } },
            { {  23,  38 }, { 135, 165 }, {  40,  54 } },
            { {  23,  38 }, { 82,  125 }, {  25,  45 } },
            { {  25,  55 }, { 8,    31 }, {  5,   23 } }
        };

        public static readonly int[,,] bottomBar = {
            { { 31, 46 }, { 17,   34 }, { 14, 39 } },
            { { 31, 45 }, { 22,   34 }, { 46, 54 } }
        };

        // Healthbar tracking shenanigans
        public static double total_life;
        public static double total_es;
        public static volatile bool showCaptureOverlay = false;

        public static int barTop, barHeight, barWidth, barLeft;
        public static int captureWidth, captureHeight, captureLeft, captureTop;
        public const int captureAreaMultiplier = 2;
        public const int colorOffset = 10;
        public const int barHorizontalOffset = 5;

        // SetWinEventHook eventArgs
        public const int EVENT_SYSTEM_MOVESIZESTART = 0x000A;
        public const int EVENT_SYSTEM_MOVESIZEEND = 0x000B;
        public const int EVENT_SYSTEM_FOREGROUND = 0x0003;
        public static volatile bool dontTrackImMoving = false;
    }
}
