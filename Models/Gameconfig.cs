namespace TowerDefense.Models
{
    /// <summary>
    /// Available difficulty levels.
    /// </summary>
    public enum Difficulty { Easy, Hard }

    /// <summary>
    /// Available game modes.
    /// </summary>
    public enum GameMode { Normal, Endless }

    /// <summary>
    /// Holds the selected difficulty and game mode for a session.
    /// Passed from ModeSelectForm to GameForm and GameController.
    /// </summary>
    public class GameConfig
    {
        public Difficulty Difficulty { get; set; }
        public GameMode GameMode { get; set; }

        // -- Derived settings based on difficulty --------------------------------
        public int StartGold => Difficulty == Difficulty.Easy ? 200 : 120;
        public int CastleHp => Difficulty == Difficulty.Easy ? 30 : 15;
        public float EnemyHpMult => Difficulty == Difficulty.Easy ? 1.0f : 1.5f;
        public float EnemySpeedMult => Difficulty == Difficulty.Easy ? 1.0f : 1.2f;

        // Max tower upgrade level per difficulty / mode combo
        public int MaxUpgradeLevel
        {
            get
            {
                if (GameMode == GameMode.Endless) return 3;
                if (Difficulty == Difficulty.Hard) return 2;
                return 1; // Normal Easy — no upgrades
            }
        }

        public int TotalWaves => GameMode == GameMode.Normal ? 10 : int.MaxValue;
    }
}
