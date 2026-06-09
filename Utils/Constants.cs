using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerDefense.Utils
{

    /// <summary>
    /// Global game constants used across the project.
    /// </summary>
    public static class Constants
    {
        // Window dimensions
        public const int MapWidth = 1200;   // 18 cols * 48
        public const int MapHeight = 1000;   // 13 rows * 48
        public const int PanelWidth = 200;
        public const int WindowWidth = MapWidth + PanelWidth;
        public const int WindowHeight = MapHeight;

        // Game loop
        public const int TargetFps = 60;
        public const int TimerIntervalMs = 1000 / TargetFps;

        // UI colours (ARGB)
        public const string FontName = "Segoe UI";
    }
}
